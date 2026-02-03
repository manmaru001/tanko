using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class JewelrySpawner_Debug : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap groundTilemap;

    [Header("Item Settings")]
    public List<GameObject> itemPrefabs;
    [Range(0f, 1f)]
    public float spawnProbability = 0.02f;
    public float spawnOffsetY = 0.0f;

    [Header("Pooling")]
    public int initialPoolPerPrefab = 10;
    public bool usePooling = true;

    [Header("Deterministic")]
    public bool useSeed = false;
    public int seed = 12345;

    [Header("Collision Check")]
    public LayerMask itemOverlapMask; // Inspector で Item レイヤーのみをセット
    public float checkRadius = 0.12f; // オーバーラップ判定半径（小さめ推奨）

    [Header("Height-based spawn multiplier")]
    [Tooltip("セルがマップ下部（bounds.yMin）にあるときの乗数。通常は 1 以上で下部を増やす")]
    public float minHeightMultiplier = 0.5f; // top side multiplier (smaller)
    [Tooltip("セルがマップ下部（bounds.yMin）にあるときの乗数。通常は min < max で下部が多くなる")]
    public float maxHeightMultiplier = 2.0f; // bottom side multiplier (bigger)
    [Tooltip("高さ補正に AnimationCurve を使うか。curve の x:0=bottom, x:1=top")]
    public bool useHeightCurve = false;
    public AnimationCurve heightMultiplierCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    System.Random rng;
    Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        if (useSeed) rng = new System.Random(seed);
        else rng = new System.Random();
    }

    void Start()
    {
        Debug.Log("[JewelrySpawner_Debug] Start() called.");
        if (usePooling) CreateInitialPools();

        Debug.LogFormat("[JewelrySpawner_Debug] groundTilemap={0}, itemPrefabs={1}, spawnProbability={2}",
            groundTilemap != null ? groundTilemap.name : "NULL",
            itemPrefabs != null ? itemPrefabs.Count.ToString() : "NULL",
            spawnProbability);

        SpawnItemsOnTilemap();
    }

    void CreateInitialPools()
    {
        if (itemPrefabs == null) return;
        foreach (var prefab in itemPrefabs)
        {
            var q = new Queue<GameObject>();
            for (int i = 0; i < initialPoolPerPrefab; i++)
            {
                var go = Instantiate(prefab);
                go.SetActive(false);
                // プール中のオブジェクトがシーン上に見えないように退避
                go.transform.position = new Vector3(9999f, 9999f, 0f);
                go.transform.SetParent(transform);
                q.Enqueue(go);
            }
            pools[prefab] = q;
        }
    }

    public void SpawnItemsOnTilemap()
    {
        if (groundTilemap == null)
        {
            Debug.LogWarning("[JewelrySpawner_Debug] groundTilemap is NULL. Aborting spawn.");
            return;
        }
        if (itemPrefabs == null || itemPrefabs.Count == 0)
        {
            Debug.LogWarning("[JewelrySpawner_Debug] itemPrefabs is NULL or empty. Aborting spawn.");
            return;
        }
        if (rng == null)
        {
            Debug.LogWarning("[JewelrySpawner_Debug] rng is NULL. Aborting spawn.");
            return;
        }

        BoundsInt bounds = groundTilemap.cellBounds;
        Debug.LogFormat("[JewelrySpawner_Debug] cellBounds: x[{0}..{1}) y[{2}..{3}) z[{4}..{5})",
            bounds.xMin, bounds.xMax, bounds.yMin, bounds.yMax, bounds.zMin, bounds.zMax);

        int checkedTiles = 0;
        int candidateTiles = 0;
        int spawned = 0;
        int skipByOverlap = 0;
        int skipByProb = 0;

        // precompute denom to normalize height
        float heightRange = Mathf.Max(1, bounds.yMax - bounds.yMin); // avoid div by zero

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                checkedTiles++;

                if (!groundTilemap.HasTile(cell)) continue;
                candidateTiles++;

                // compute height-based multiplier:
                // normalizedHeight: 0 at bottom (bounds.yMin) -> 1 at top (bounds.yMax-1)
                float normalizedHeight = (y - bounds.yMin) / heightRange; // 0..~1
                // we want lower y (normalizedHeight small) to increase spawn. So invert mapping:
                float t = Mathf.Clamp01(normalizedHeight); // 0..1
                float multiplier;
                if (useHeightCurve)
                {
                    // curve x=0 bottom, x=1 top. But curve returns factor relative to 1, so we map to multiplier range.
                    float curveVal = heightMultiplierCurve.Evaluate(t); // user-defined
                    // map curveVal(assumed 0..1 or arbitrary) to range [minHeightMultiplier..maxHeightMultiplier]
                    // If curve is 0..1, this maps 0->min, 1->max. If curve returns outside, it still maps linearly.
                    multiplier = Mathf.Lerp(minHeightMultiplier, maxHeightMultiplier, 1f - t * 0f + curveVal); // keep simple mapping
                    // simpler: multiplier = Mathf.Lerp(maxHeightMultiplier, minHeightMultiplier, t) * curveVal;
                    // but to avoid surprises, use next simpler variant:
                    multiplier = Mathf.Lerp(maxHeightMultiplier, minHeightMultiplier, t) * curveVal;
                }
                else
                {
                    // linear map: bottom -> maxHeightMultiplier, top -> minHeightMultiplier
                    multiplier = Mathf.Lerp(maxHeightMultiplier, minHeightMultiplier, t);
                }

                // effective probability (clamp 0..1)
                float effectiveProb = Mathf.Clamp01(spawnProbability * multiplier);

                double roll = rng.NextDouble();
                if (roll > effectiveProb)
                {
                    skipByProb++;
                    continue;
                }

                Vector3 pos = groundTilemap.GetCellCenterWorld(cell);
                pos.y += spawnOffsetY;

                Collider2D hit = null;
                if (itemOverlapMask.value != 0)
                {
                    hit = Physics2D.OverlapPoint(pos, itemOverlapMask);
                    // hit = Physics2D.OverlapCircle(pos, checkRadius, itemOverlapMask);
                }
                else
                {
                    hit = Physics2D.OverlapCircle(pos, checkRadius);
                }

                if (hit != null)
                {
                    skipByOverlap++;
                    continue;
                }

                int idx = rng.Next(0, itemPrefabs.Count);
                var prefab = itemPrefabs[idx];

                var go = GetFromPool(prefab);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
                go.SetActive(true);
                spawned++;

                if (spawned <= 20)
                {
                    Debug.LogFormat("[JewelrySpawner_Debug] Spawned prefab {0} at cell {1} pos {2} (mult={3:0.00} effProb={4:0.000})",
                        prefab.name, cell, pos, multiplier, effectiveProb);
                }
            }
        }

        Debug.LogFormat("[JewelrySpawner_Debug] Done. checked={0}, candidates={1}, spawned={2}, skipProb={3}, skipOverlap={4}",
            checkedTiles, candidateTiles, spawned, skipByProb, skipByOverlap);
    }

    GameObject GetFromPool(GameObject prefab)
    {
        if (!usePooling) return Instantiate(prefab);
        if (!pools.ContainsKey(prefab)) pools[prefab] = new Queue<GameObject>();
        var q = pools[prefab];
        if (q.Count > 0)
        {
            var go = q.Dequeue();
            go.SetActive(true);
            return go;
        }
        var newGo = Instantiate(prefab);
        return newGo;
    }

    [ContextMenu("SpawnOneRandom")]
    public void SpawnOneRandomForDebug()
    {
        if (groundTilemap == null)
        {
            Debug.LogWarning("[JewelrySpawner_Debug] groundTilemap is NULL (SpawnOneRandom).");
            return;
        }
        BoundsInt bounds = groundTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!groundTilemap.HasTile(cell)) continue;
                Vector3 pos = groundTilemap.GetCellCenterWorld(cell);
                int idx = rng.Next(0, itemPrefabs.Count);
                var go = GetFromPool(itemPrefabs[idx]);
                go.transform.position = pos;
                go.SetActive(true);
                Debug.Log("[JewelrySpawner_Debug] SpawnOneRandomForDebug spawned at " + cell);
                return;
            }
        }
        Debug.Log("[JewelrySpawner_Debug] SpawnOneRandomForDebug found no tile to spawn on.");
    }
}

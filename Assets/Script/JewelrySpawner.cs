using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


public class JewelrySpawner_Debug : MonoBehaviour
{
    [Header("Tilemap Settings")]
    // 探索対象の Tilemap（Inspector にアサイン）
    public Tilemap groundTilemap;

    [Header("Item Settings")]
    // スポーンするアイテムのプレハブリスト
    public List<GameObject> itemPrefabs;
    [Range(0f, 1f)]
    // 基本のセルごとの出現確率（0..1）
    public float spawnProbability = 0.02f;
    // スポーン位置をセル中心から上下にずらしたい場合のオフセット
    public float spawnOffsetY = 0.0f;

    [Header("Pooling")]
    // 各プレハブあたりの初期プールサイズ（Instantiate を抑えるため）
    public int initialPoolPerPrefab = 10;
    // プーリングを使うかどうか
    public bool usePooling = true;

    [Header("Deterministic")]
    // シードを固定して同じ配置を再現したい場合
    public bool useSeed = false;
    public int seed = 12345;

    [Header("Collision Check")]
    // オーバーラップ判定で参照するレイヤー（Item レイヤーのみなどに限定する）
    public LayerMask itemOverlapMask; // Inspector で Item レイヤーのみをセット
    // オーバーラップ半径（OverlapCircle を使う場合に参照）
    public float checkRadius = 0.12f; // オーバーラップ判定半径（小さめ推奨）

    [Header("Height-based spawn multiplier")]
    [Tooltip("セルがマップ下部（bounds.yMin）にあるときの乗数。通常は 1 以上で下部を増やす")]
    // マップ上部と下部での確率を変えるための最小・最大乗数
    public float minHeightMultiplier = 0.5f; // top side multiplier (smaller)
    [Tooltip("セルがマップ下部（bounds.yMin）にあるときの乗数。通常は min < max で下部が多くなる")]
    public float maxHeightMultiplier = 2.0f; // bottom side multiplier (bigger)
    [Tooltip("高さ補正に AnimationCurve を使うか。curve の x:0=bottom, x:1=top")]
    // 高さ補正を曲線で行いたい場合に使用（false の場合は線形補間）
    public bool useHeightCurve = false;
    // エディタから曲線をいじれるようにしておく（デフォルトは直線）
    public AnimationCurve heightMultiplierCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    // 内部用: 決定論的乱数生成器（System.Random を使用）
    System.Random rng;
    // プール管理（プレハブ -> インスタンスキュー）
    Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    // Awake: Start より前に rng を初期化しておく（他から早期に呼ばれる可能性に備える）
    void Awake()
    {
        if (useSeed) rng = new System.Random(seed);
        else rng = new System.Random();
    }

    // Start: 初期化と初回スポーン呼び出し
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

    // プールを作る。非アクティブで退避しておく。
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

    /// <summary>
    /// マップ全体を走査してアイテムを配置するメイン処理。
    /// - Tilemap の cellBounds を走査
    /// - タイルがあるセルを候補にし、高さに応じた倍率を算出して確率判定
    /// - オーバーラップ（既存オブジェクト）をレイヤー限定でチェック
    /// - プールからインスタンスを取り出して配置
    /// </summary>
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

        // 高さ正規化用の分母を事前計算（0除算を避ける）
        float heightRange = Mathf.Max(1, bounds.yMax - bounds.yMin); // avoid div by zero

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                checkedTiles++;

                // タイルが無ければ候補外
                if (!groundTilemap.HasTile(cell)) continue;
                candidateTiles++;

                // 高さに応じた倍率を計算する
                // normalizedHeight: 0 が下端(bounds.yMin)、1 が上端に近い位置
                float normalizedHeight = (y - bounds.yMin) / heightRange; // 0..~1
                float t = Mathf.Clamp01(normalizedHeight); // 0..1 にクランプ
                float multiplier;
                if (useHeightCurve)
                {
                    // 曲線を使う場合：curve の値を乗算に使いつつ上下の範囲にマッピング
                    float curveVal = heightMultiplierCurve.Evaluate(t);
                    // 曲線値を元に、bottom->top を max->min に線形補間して乗数化
                    multiplier = Mathf.Lerp(maxHeightMultiplier, minHeightMultiplier, t) * curveVal;
                }
                else
                {
                    // 線形補間：下（t=0）で maxHeightMultiplier、上（t=1）で minHeightMultiplier
                    multiplier = Mathf.Lerp(maxHeightMultiplier, minHeightMultiplier, t);
                }

                // 最終的な有効確率（0..1）にクランプ
                float effectiveProb = Mathf.Clamp01(spawnProbability * multiplier);

                // 乱数判定
                double roll = rng.NextDouble();
                if (roll > effectiveProb)
                {
                    skipByProb++;
                    continue;
                }

                // セル中心のワールド座標（必要に応じてオフセット）
                Vector3 pos = groundTilemap.GetCellCenterWorld(cell);
                pos.y += spawnOffsetY;

                // オーバーラップ判定（指定レイヤーのみチェックして地形のコライダーを無視する）
                Collider2D hit = null;
                if (itemOverlapMask.value != 0)
                {
                    // OverlapPoint は点での当たりをチェックするため軽量
                    hit = Physics2D.OverlapPoint(pos, itemOverlapMask);
                    // もし半径でチェックしたければ以下を代わりに使う
                    // hit = Physics2D.OverlapCircle(pos, checkRadius, itemOverlapMask);
                }
                else
                {
                    // マスクが未設定の場合のフォールバック（地形に引っかかる可能性あり）
                    hit = Physics2D.OverlapCircle(pos, checkRadius);
                }

                if (hit != null)
                {
                    // 既に同レイヤー上にオブジェクトがあるため生成をスキップ
                    skipByOverlap++;
                    continue;
                }

                // プレハブをランダム選択してプールから取得、配置
                int idx = rng.Next(0, itemPrefabs.Count);
                var prefab = itemPrefabs[idx];

                var go = GetFromPool(prefab);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
                go.SetActive(true);
                spawned++;

                // ログは上限を設けている（大量生成時のログ爆発を防ぐ）
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

    // プールからオブジェクトを取り出す（無ければ Instantiate）
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

    // コンテキストメニューから手動で一個だけスポーンできるデバッグ用メソッド
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

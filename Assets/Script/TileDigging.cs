using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

/// <summary>
/// TileDigging
/// - 既存の DigAtPlayer / DigArea 処理はそのまま維持
/// - 起動時に Tilemap の初期状態をバックアップしておき、ResetToInitial() で復元可能にする
/// - TilemapCollider2D / CompositeCollider2D を再生成するための補助処理を含む
/// </summary>
public class TileDigging : MonoBehaviour
{
    public Tilemap groundTilemap;       // Inspector にアタッチ
    public ParticleSystem digParticles; // optional
    public float digRange = 1.0f;       // 掘る距離（プレイヤー基準）
    public Vector3 digOffset = new Vector3(0f, -1f, 0f); // 掘る方向オフセット例

    // サウンド（使いたければここに PlaySFX 呼び出しを入れてもよい）
    public GameObject SoundManager;

    // === 初期状態バックアップ用 ===
    private BoundsInt originalBounds;
    private TileBase[] originalTiles;
    private bool hasBackup = false;

    // タイルマップのコライダー（あれば）
    private TilemapCollider2D tilemapCollider;
    private CompositeCollider2D compositeCollider;

    // Awake でバックアップ（Start より早くやりたい場合は Awake）
    void Awake()
    {
        if (groundTilemap == null) groundTilemap = GetComponent<Tilemap>();
        if (groundTilemap == null)
        {
            Debug.LogWarning("TileDigging: groundTilemap が設定されていません。");
            return;
        }

        // バックアップを作成
        originalBounds = groundTilemap.cellBounds;
        originalTiles = groundTilemap.GetTilesBlock(originalBounds);
        hasBackup = (originalTiles != null && originalTiles.Length > 0);

        // コライダー取得（あれば）
        tilemapCollider = groundTilemap.GetComponent<TilemapCollider2D>();
        compositeCollider = groundTilemap.GetComponent<CompositeCollider2D>();
    }

    // ===========================
    // 既存の掘削処理（変更なし）
    // ===========================

    /// <summary>
    /// プレイヤーの位置と方向から単セル掘る。掘れたら true を返す。
    /// </summary>
    public bool DigAtPlayer(Vector3 playerWorldPos, Vector2 direction)
    {
        if (groundTilemap == null) return false;

        Vector3 targetWorld = playerWorldPos + (Vector3)direction.normalized * digRange + digOffset;
        Vector3Int cell = groundTilemap.WorldToCell(targetWorld);

        TileBase tile = groundTilemap.GetTile(cell);
        if (tile != null)
        {
            groundTilemap.SetTile(cell, null);
            groundTilemap.RefreshTile(cell);

            if (digParticles != null)
            {
                Vector3 worldCenter = groundTilemap.GetCellCenterWorld(cell);
                Instantiate(digParticles, worldCenter, Quaternion.identity);
            }

            return true;
        }
        return false;
    }

    /// <summary>
    /// 範囲掘削（brushRadius の正方形領域）。壊したタイル数を返す。
    /// </summary>
    public int DigArea(Vector3 worldCenter, int brushRadius)
    {
        if (groundTilemap == null) return 0;

        int removed = 0;
        Vector3Int centerCell = groundTilemap.WorldToCell(worldCenter);
        for (int x = -brushRadius; x <= brushRadius; x++)
        {
            for (int y = -brushRadius; y <= brushRadius; y++)
            {
                Vector3Int c = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z);
                if (groundTilemap.GetTile(c) != null)
                {
                    groundTilemap.SetTile(c, null);
                    groundTilemap.RefreshTile(c);

                    if (digParticles != null)
                    {
                        Vector3 worldCenterTile = groundTilemap.GetCellCenterWorld(c);
                        Instantiate(digParticles, worldCenterTile, Quaternion.identity);
                    }

                    removed++;
                }
            }
        }
        return removed;
    }

    /// <summary>
    /// 初期状態にリセットする
    /// </summary>
    public void ResetToInitial()
    {
        if (!hasBackup) return;

        // タイル復元
        groundTilemap.ClearAllTiles();
        groundTilemap.SetTilesBlock(originalBounds, originalTiles);
        groundTilemap.RefreshAllTiles();

        // Collider 再構築（超重要）
        StartCoroutine(ForceRebuildCollider());
    }

    IEnumerator ForceRebuildCollider()
    {
        // Tilemap内部更新待ち
        yield return null;
        yield return null; // 2フレ待つとさらに安定

        // TilemapCollider 再生成
        if (tilemapCollider != null)
        {
            tilemapCollider.enabled = false;
            yield return null;
            tilemapCollider.enabled = true;
        }

        // Composite 再生成
        if (compositeCollider != null)
        {
            compositeCollider.GenerateGeometry();
        }

        // Rigidbody 物理再同期
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
            yield return null;
            rb.simulated = true;
        }

        Physics2D.SyncTransforms();
    }

    // Optional: 直接全消ししてから初期状態に戻すユーティリティ（必要なら呼ぶ）
    public void ClearAndResetToInitial()
    {
        ResetToInitial();
    }

    // Optional: バックアップを強制的に再取得する（例えば Editor 上で手で地形を変えたとき用）
    public void RebuildInitialBackup()
    {
        if (groundTilemap == null) return;
        originalBounds = groundTilemap.cellBounds;
        originalTiles = groundTilemap.GetTilesBlock(originalBounds);
        hasBackup = (originalTiles != null && originalTiles.Length > 0);
        Debug.Log("TileDigging: 初期バックアップを再作成しました。");
    }
}

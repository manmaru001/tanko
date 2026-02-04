using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDigging : MonoBehaviour
{
    public Tilemap groundTilemap;       // Inspector にアタッチ
    public ParticleSystem digParticles; // optional
    public float digRange = 1.0f;       // 掘る距離（プレイヤー基準）
    public Vector3 digOffset = new Vector3(0f, -1f, 0f); // 掘る方向オフセット例

    // サウンド（使いたければここに PlaySFX 呼び出しを入れてもよい）
    public GameObject SoundManager;

    /// <summary>
    /// プレイヤーの位置と方向から単セル掘る。掘れたら true を返す。
    /// </summary>
    public bool DigAtPlayer(Vector3 playerWorldPos, Vector2 direction)
    {
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
}

using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDigging : MonoBehaviour
{
    public Tilemap groundTilemap;       // Inspector にアタッチ
    public ParticleSystem digParticles; // optional
    public float digRange = 1.0f;       // 掘る距離（プレイヤー基準）
    public Vector3 digOffset = new Vector3(0f, -1f, 0f); // 掘る方向オフセット例

    // プレイヤーが掘ると呼ぶ（例：ボタン押しで）
    public void DigAtPlayer(Vector3 playerWorldPos, Vector2 direction)
    {
        // 掘る対象のワールド座標（方向 + 範囲 を使って決める）
        Vector3 targetWorld = playerWorldPos + (Vector3)direction.normalized * digRange + digOffset;

        // ワールド座標 → セル座標
        Vector3Int cell = groundTilemap.WorldToCell(targetWorld);

        // 現在のタイルを取得
        TileBase tile = groundTilemap.GetTile(cell);
        if (tile != null)
        {
            // タイル削除
            groundTilemap.SetTile(cell, null);

            // 周囲のルールタイル等をリフレッシュしたい場合
            groundTilemap.RefreshTile(cell);

            // パーティクルをセルの中心で出す（任意）
            if (digParticles != null)
            {
                Vector3 worldCenter = groundTilemap.GetCellCenterWorld(cell);
                Instantiate(digParticles, worldCenter, Quaternion.identity);
            }

            // アイテムドロップや音の再生などをここに
            // DropItem(cell);
            // AudioSource.PlayClipAtPoint(digSound, worldCenter);
        }
    }

    // 範囲掘削（ツルハシのサイズが大きいとき）
    public void DigArea(Vector3 worldCenter, int brushRadius)
    {
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
                }
            }
        }
    }
}

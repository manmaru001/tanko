using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDeleteContoroller : MonoBehaviour
{
    public Tilemap tilemap;                     // 対象Tilemap（Inspectorで割当）
    public GameObject breakEffectPrefab;        // 破壊時パーティクル等（任意）
    public GameObject dropPrefab;               // ドロップ（任意）

    // Composite 更新を行うか（頻繁に破壊するなら false にして別途まとめて更新することを推奨）
    public bool rebuildCompositeEachTime = true;

    // --- 即時破壊用メソッド ---

    // ワールド座標からセルを決めて即座に削除する（プレイヤーから呼ぶ）
    public void DamageCellAtWorld(Vector3 worldPos)
    {
        if (tilemap == null) return;

        // 攻撃点 → セル座標
        Vector3Int cell = tilemap.WorldToCell(worldPos);

        // まずそのセル
        if (tilemap.HasTile(cell))
        {
            DestroyCellImmediate(cell);
            return;
        }

        // 周囲8マスも探す（ズレ対策）
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int c = cell + new Vector3Int(x, y, 0);

                if (tilemap.HasTile(c))
                {
                    DestroyCellImmediate(c);
                    return;
                }
            }
        }

        Debug.Log("No tile near " + cell);
    }


    // Debug 用：World座標 -> セル の変換結果と周辺セルの有無をログ
    private void DebugCellLookup(Vector3 worldPos)
    {

        if (tilemap == null)
        {
            Debug.LogWarning("TileDeleteContoroller: tilemap is null");
            return;
        }

        Vector3Int cell = tilemap.WorldToCell(worldPos);
        Vector3 cellCenter = tilemap.CellToWorld(cell) + (Vector3)(tilemap.cellSize * 0.5f);
        Debug.Log($"WorldPos: {worldPos} -> cell: {cell} , cellCenter: {cellCenter} , HasTile: {tilemap.HasTile(cell)}");

        // 周辺セル(3x3)の状況も出す（攻撃点が境界にいると誤セルになる場合の判定用）
        for (int y = 1; y >= -1; y--)
        {
            string line = "";
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int c = cell + new Vector3Int(x, y, 0);
                line += tilemap.HasTile(c) ? "[X]" : "[ ]";
            }
            Debug.Log($"Row {y}: {line}");
        }
    }

    // セル座標で即座に削除する
    public void DestroyCellImmediate(Vector3Int cell)
    {
        TileBase tile = tilemap.GetTile(cell);
        if (tile == null) return; // 既に空なら何もしない

        // 破壊エフェクト・ドロップ（セル中心にスポーン）
        Vector3 cellCenter = tilemap.CellToWorld(cell) + (Vector3)(tilemap.cellSize * 0.5f);
        if (breakEffectPrefab != null)
            Instantiate(breakEffectPrefab, cellCenter, Quaternion.identity);

        if (dropPrefab != null)
            Instantiate(dropPrefab, cellCenter, Quaternion.identity);

        // タイル削除
        tilemap.SetTile(cell, null);
        tilemap.RefreshTile(cell);

        // Composite を使っている場合はコライダの再構築（オプション）
        if (rebuildCompositeEachTime)
            StartCoroutine(ForceRebuildComposite(tilemap));
    }



    // 便利メソッド：ワールド座標からセル中心ワールド座標を取得して返す
    public Vector3 GetCellCenterWorld(Vector3 worldPos)
    {
        if (tilemap == null) return worldPos;
        Vector3Int cell = tilemap.WorldToCell(worldPos);
        return tilemap.CellToWorld(cell) + (Vector3)(tilemap.cellSize * 0.5f);
    }

    // Composite を使っているときのコライダ更新対策（必要ならオン／オフ）
    private IEnumerator ForceRebuildComposite(Tilemap t)
    {
        var tilemapCollider = t.GetComponent<TilemapCollider2D>();
        var composite = t.GetComponent<CompositeCollider2D>();
        if (tilemapCollider != null && composite != null)
        {
            tilemapCollider.enabled = false;
            // 1フレーム待つ（Unityの内部更新を確実に促す）
            yield return null;
            tilemapCollider.enabled = true;
        }
        else
        {
            yield return null;
        }
    }
}

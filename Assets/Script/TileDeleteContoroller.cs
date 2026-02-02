using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileDeleteContoroller : MonoBehaviour
{
    public Tilemap tilemap;                     // 対象Tilemap（Inspectorで割当）
    public TileBase[] damageTiles;              // ひび割れ段階用タイル（段階数 = damageTiles.Length）
    public GameObject breakEffectPrefab;        // 破壊時パーティクル等（任意）
    public GameObject dropPrefab;               // ドロップ（任意）

    // damageTiles.Length = n のとき、maxHP = n + 1 にする（n段階目で壊れる）
    public int defaultMaxHP = 3;

    // 壊れかけセルだけを保持
    private Dictionary<Vector3Int, int> hpMap = new Dictionary<Vector3Int, int>();

    // ワールド座標からセルを決めてダメージを与える（プレイヤーから呼ぶ）
    public void DamageCellAtWorld(Vector3 worldPos, int damage = 1)
    {
        if (tilemap == null) return;
        Vector3Int cell = tilemap.WorldToCell(worldPos);
        DamageCell(cell, damage);
    }

    // セル座標でダメージを与える
    public void DamageCell(Vector3Int cell, int damage = 1)
    {
        TileBase tile = tilemap.GetTile(cell);
        if (tile == null) return; // 空気なら何もしない

        if (!hpMap.ContainsKey(cell))
        {
            // 初期HP 設定（damageTiles の段階に合わせて設定しておく）
            hpMap[cell] = defaultMaxHP;
        }

        hpMap[cell] -= damage;

        if (hpMap[cell] > 0)
        {
            // ヒビ表現があるなら段階タイルへ差し替え
            if (damageTiles != null && damageTiles.Length > 0)
            {
                // stage = 1 .. damageTiles.Length  (1が軽いヒビ)
                int stage = Mathf.Clamp(defaultMaxHP - hpMap[cell], 1, damageTiles.Length);
                TileBase damageTile = damageTiles[Mathf.Clamp(stage - 1, 0, damageTiles.Length - 1)];
                tilemap.SetTile(cell, damageTile);
                tilemap.RefreshTile(cell);
            }
        }
        else
        {
            // 破壊
            if (breakEffectPrefab != null)
                Instantiate(breakEffectPrefab, tilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f), Quaternion.identity);

            if (dropPrefab != null)
                Instantiate(dropPrefab, tilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f), Quaternion.identity);

            tilemap.SetTile(cell, null);
            tilemap.RefreshTile(cell);
            hpMap.Remove(cell);
        }

        // Composite を使っているときのコライダ更新対策
        // （TilemapCollider2D.enabled を一度オフ→次フレームにオンにする）
        if (tilemap != null)
            StartCoroutine(ForceRebuildComposite(tilemap)); // ← 修正点
    }

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

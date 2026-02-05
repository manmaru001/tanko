using System.Collections;
using UnityEngine;

/// <summary>
/// BombController
/// - TileDigging を使って地形を範囲破壊するボム用スクリプト
/// - 爆発範囲内の Jewelry をすべて処理（ExplodeJewelry を呼ぶ）
/// - 爆発範囲内に Player がいるかを判定するメソッドを追加
/// </summary>
public class BombController : MonoBehaviour
{
    [Header("Fuse / Explosion")]
    public float fuseTime = 3.0f;            // 点火から爆発までの時間（秒）
    const int blastRadiusTiles = 6;         // 基準となるタイル半径（定数）
    public bool explodeOnStart = false;      // スポーン時に自動で点火するか
    [Tooltip("blastRadiusTiles に掛ける倍率。例: 1.0 = blastRadiusTiles、2.0 = 2倍")]
    public float explodeRange = 1.0f;        // 爆風範囲の倍率（タイル単位の倍率）

    [Header("References")]
    public TileDigging tileDigging;          // TileDigging（Inspector または自動取得）
    public GameObject explosionEffect;       // 爆発パーティクル（任意）
    public AudioClip explosionSound;         // 爆発音（任意、SoundManager が無いときのフォールバック）
    public LayerMask enemyLayer;             // 敵用（任意）
    public float enemyDamage = 10f;          // 敵へのダメージ（任意）
    public LayerMask rigidbodyLayer;         // 物理オブジェクトへ力を与えるレイヤー（任意）
    public float explosionForce = 300f;      // 爆風の基本力（任意）
    public SoundManager SoundManager;        // （シーン上の SoundManager コンポーネントを参照）

    [Header("Destroy / Player / Jewelry Layers")]
    [Tooltip("爆発で消したい Jewelry を置いているレイヤーをセット")]
    public LayerMask jewelryLayer;           // Jewelry 用レイヤー
    [Tooltip("Player を検出するためのレイヤー（Player が属するレイヤーをセット）")]
    public LayerMask playerLayer;            // Player のレイヤー（検出用）

    [Header("Behavior")]
    public bool usePooling = false;          // プール利用時は true（ここでは単純 Destroy）
    public float destroyDelay = 0.1f;        // 爆発後にオブジェクトを消すまでの猶予

    // 内部
    bool isArmed = false;

    void Start()
    {
        // Inspector で未設定ならシーンから自動取得（プレハブに保持したくない場合に有用）
        if (SoundManager == null)
        {
            SoundManager = FindFirstObjectByType<SoundManager>();
            if (SoundManager == null)
                Debug.LogWarning("BombController: SoundManager が見つかりません（シーンに存在しますか？）");
        }

        if (tileDigging == null)
        {
            tileDigging = FindFirstObjectByType<TileDigging>();
        }

        if (explodeOnStart) Ignite();
    }

    /// <summary> 外部から点火する用（プレイヤーが置いたときなど） </summary>
    public void Ignite()
    {
        if (isArmed) return;
        isArmed = true;
        StartCoroutine(FuseCoroutine());
    }

    /// <summary> 即時爆発（デバッグ用） </summary>
    public void ExplodeImmediate()
    {
        if (isArmed) return;
        isArmed = true;
        StopAllCoroutines();
        StartCoroutine(ExplodeAndCleanup());
    }

    /// <summary> 点火から爆発までのコルーチン </summary>
    IEnumerator FuseCoroutine()
    {
        yield return new WaitForSeconds(fuseTime);
        yield return ExplodeAndCleanup();
    }

    /// <summary> 爆発処理と後処理 </summary>
    IEnumerator ExplodeAndCleanup()
    {
        // 1) エフェクト
        if (explosionEffect != null) Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // サウンド：優先して SoundManager 経由で鳴らす（プロジェクトで PlaySFX("Sound_Bomb") を登録している前提）
        if (SoundManager != null)
        {
            SoundManager.PlaySFX("Sound_Bomb");
        }
        else if (explosionSound != null)
        {
            // フォールバック：AudioSource.PlayClipAtPoint（簡易再生）
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
        }

        // タイル半径を計算（切り上げで整数化）
        int tileRadius = Mathf.Max(0, Mathf.CeilToInt(blastRadiusTiles * explodeRange));

        // ワールド半径を計算（セルサイズ × タイル数）
        float worldRadius = ComputeWorldRadiusForTileRadius(tileRadius);

        // 2) TileDigging を使ってタイルを破壊（タイル単位の半径を渡す）
        if (tileDigging != null && tileDigging.groundTilemap != null)
        {
            tileDigging.DigArea(transform.position, tileRadius);
        }
        else
        {
            Debug.LogWarning("BombController: TileDigging がアサインされていません。タイル破壊は行われません。");
        }

        // 2.5) Jewelry を爆発範囲内から処理（ExplodeJewelry を呼ぶ）
        if (jewelryLayer != 0)
        {
            Collider2D[] jewels = Physics2D.OverlapCircleAll(transform.position, worldRadius, jewelryLayer);
            foreach (var j in jewels)
            {
                if (j == null || j.gameObject == null) continue;

                var jc = j.GetComponent<JewelryController>();
                if (jc != null)
                {
                    jc.ExplodeJewelry();
                }
                else
                {
                    // JewelryController が無ければフォールバックで削除
                    Destroy(j.gameObject);
                }
            }
        }

        // 3) 敵ダメージ等（必要なら実装）

        // 4) 物理オブジェクトへの爆風力（減衰あり）
        if (rigidbodyLayer != 0 && explosionForce > 0f)
        {
            var cols = Physics2D.OverlapCircleAll(transform.position, worldRadius, rigidbodyLayer);
            foreach (var c in cols)
            {
                var rb = c.attachedRigidbody;
                if (rb == null) continue;
                Vector2 dir = (rb.position - (Vector2)transform.position);
                float dist = Mathf.Max(0.001f, dir.magnitude);
                float atten = Mathf.Clamp01(1f - (dist / worldRadius));
                rb.AddForce(dir.normalized * explosionForce * atten);
            }
        }

        // 5) プレイヤー検出（必要ならここで処理）
        Collider2D[] players = Physics2D.OverlapCircleAll(transform.position, worldRadius, playerLayer);
        if (players != null && players.Length > 0)
        {
            foreach (var pcol in players)
            {
                if (pcol == null) continue;
                var pc = pcol.GetComponent<PlayerController>();
                if (pc != null)
                {
                    // PlayerController に専用メソッドを作っておき、そこへ通知する
                    pc.OnBombHit();
                }
                else
                {
                    // プレイヤーが別コンポーネント構成なら別対応
                    Debug.Log("BombController: プレイヤーの PlayerController が見つかりません。");
                }
            }
        }

        // 6) 後処理（爆弾の削除 / プール戻し）
        if (usePooling)
        {
            gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 指定のタイル半径（整数）を元にワールド半径を返す。
    /// </summary>
    float ComputeWorldRadiusForTileRadius(int tileRadius)
    {
        if (tileDigging != null && tileDigging.groundTilemap != null)
        {
            Vector3 cellSize = tileDigging.groundTilemap.cellSize;
            float cell = Mathf.Max(cellSize.x, cellSize.y);
            return tileRadius * cell;
        }
        // フォールバック（tileRadius が 0 の場合は 0）
        return tileRadius * 1f;
    }

    // Sceneビューで爆風範囲を可視化
    void OnDrawGizmosSelected()
    {
        int tileRadius = Mathf.Max(0, Mathf.CeilToInt(blastRadiusTiles * explodeRange));
        float worldR = (tileDigging != null && tileDigging.groundTilemap != null) ? ComputeWorldRadiusForTileRadius(tileRadius) : tileRadius;
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, worldR);
    }
}

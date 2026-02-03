using System.Collections;
using UnityEngine;

/// <summary>
/// BombController
/// - TileDigging を使って地形を範囲破壊するボム用スクリプト
/// - プレハブにアタッチして、InspectorでTileDigging等をセットして使う
/// </summary>
public class BombController : MonoBehaviour
{
    [Header("Fuse / Explosion")]
    public float fuseTime = 1.2f;            // 点火から爆発までの時間（秒）
    public int blastRadiusTiles = 2;         // タイル単位の爆風半径（例: 2 => 半径2の正方形/円相当）
    public bool explodeOnStart = false;      // スポーン時に自動で点火するか

    [Header("References")]
    public TileDigging tileDigging;          // TileDigging を Inspector でセット（未設定なら自動検索）
    public GameObject explosionEffect;       // 爆発パーティクル（任意）
    public AudioClip explosionSound;         // 爆発音（任意）
    public LayerMask enemyLayer;             // 敵を指定してダメージを与えるならセット
    public float enemyDamage = 10f;          // 敵へのダメージ値（任意）
    public LayerMask rigidbodyLayer;         // 物理オブジェクトへ力を与えるレイヤー（任意）
    public float explosionForce = 300f;      // 一番近い位置での押し出し力（任意）

    [Header("Behavior")]
    public bool usePooling = false;          // プールを使う場合 true にして、呼び出し元で非アクティブに戻す
    public float destroyDelay = 0.1f;        // 爆発後にオブジェクトを消す／プールに戻すまでの猶予

    // 内部
    bool isArmed = false;

    void Start()
    {
        // TileDigging がセットされていなければシーン中から探す（簡易フォールバック）
        if (tileDigging == null)
        {
            tileDigging = FindObjectOfType<TileDigging>();
        }

        if (explodeOnStart)
        {
            Ignite();
        }
    }

    /// <summary>
    /// 外部から点火する用（プレイヤーが置いたときなど）
    /// </summary>
    public void Ignite()
    {
        if (isArmed) return;
        isArmed = true;
        StartCoroutine(FuseCoroutine());
    }

    /// <summary>
    /// 即時爆発（デバッグ用）
    /// </summary>
    public void ExplodeImmediate()
    {
        if (isArmed) return;
        isArmed = true;
        StopAllCoroutines();
        StartCoroutine(ExplodeAndCleanup());
    }

    IEnumerator FuseCoroutine()
    {
        // （将来）点火エフェクトや点滅をここで入れられる
        yield return new WaitForSeconds(fuseTime);
        yield return ExplodeAndCleanup();
    }

    IEnumerator ExplodeAndCleanup()
    {
        // 1) エフェクト・音
        if (explosionEffect != null)
        {
            var fx = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            // 自動破棄されるパーティクルなら何もしない、そうでなければ適宜 Destroy
        }
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // 2) TileDigging を使ってタイルを破壊
        if (tileDigging != null && tileDigging.groundTilemap != null)
        {
            // DigArea はセル中心の worldCenter を受け取りタイル半径（int）で破壊する想定
            tileDigging.DigArea(transform.position, blastRadiusTiles);

            // （必要なら）Tilemap のコライダー更新を強制
            // tileDigging.groundTilemap.RefreshAllTiles();
        }
        else
        {
            Debug.LogWarning("BombController: TileDigging がアサインされていません。タイル破壊は行われません。");
        }

        // 3) 敵へのダメージ（爆風半径をワールド座標で算出して OverlapCircleAll）
        float worldRadius = ComputeWorldRadius();
        //if (enemyLayer != 0)
        //{
        //    var hits = Physics2D.OverlapCircleAll(transform.position, worldRadius, enemyLayer);
        //    foreach (var col in hits)
        //    {
        //        // 敵に対するダメージ処理はプロジェクト側のインターフェースに合わせて実装してください。
        //        // ここではIFakeDamageReceiver的なコンポーネントがあると仮定して呼ぶ例：
        //        var hp = col.GetComponent<Health>(); // Health クラスがあればそれに合わせる
        //        if (hp != null)
        //        {
        //            hp.TakeDamage(enemyDamage);
        //        }
        //    }
        //}

        // 4) 物理オブジェクトへの爆風力（減衰あり）
        if (rigidbodyLayer != 0 && explosionForce > 0f)
        {
            var cols = Physics2D.OverlapCircleAll(transform.position, worldRadius, rigidbodyLayer);
            foreach (var c in cols)
            {
                var rb = c.attachedRigidbody;
                if (rb != null)
                {
                    Vector2 dir = (rb.position - (Vector2)transform.position);
                    float dist = dir.magnitude;
                    if (dist <= 0.001f) dist = 0.001f;
                    float atten = Mathf.Clamp01(1f - (dist / worldRadius)); // 近いほど強く
                    rb.AddForce(dir.normalized * explosionForce * atten);
                }
            }
        }

        // 5) 後処理（爆弾を破壊 or 非活性化）
        if (usePooling)
        {
            // プール方式なら非アクティブにして呼び出し元に戻す
            gameObject.SetActive(false);
        }
        else
        {
            // 少し待ってから破壊（エフェクトの再生が終わるのを待つ）
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// blastRadiusTiles（タイル数）を基にワールド座標の半径を近似計算して返す
    /// Tilemap の cellSize を参照して算出（TileDigging が持つ groundTilemap を利用）
    /// </summary>
    float ComputeWorldRadius()
    {
        if (tileDigging != null && tileDigging.groundTilemap != null)
        {
            Vector3 cellSize = tileDigging.groundTilemap.cellSize;
            // 正方格子を仮定して X を使う。円形近似のため *0.5 を使って中心からの距離にする
            float cell = Mathf.Max(cellSize.x, cellSize.y);
            return blastRadiusTiles * cell;
        }
        // フォールバック: タイル未設定ならタイル数をそのまま単位にする
        return blastRadiusTiles * 1f;
    }

    // Sceneビューで爆風範囲を可視化
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.4f);
        float r = blastRadiusTiles;
        // ワールド半径表示（おおよそ）
        float worldR = (tileDigging != null && tileDigging.groundTilemap != null) ? ComputeWorldRadius() : blastRadiusTiles;
        Gizmos.DrawWireSphere(transform.position, worldR);
    }
}

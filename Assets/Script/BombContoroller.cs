using System.Collections;
using UnityEngine;

/// <summary>
/// BombController
/// - TileDigging を使って地形を範囲破壊するボム用スクリプト
/// - 爆発範囲内の Jewelry をすべて削除する処理を追加
/// - 爆発範囲内に Player がいるかを判定するメソッドを追加
/// </summary>
public class BombController : MonoBehaviour
{
    [Header("Fuse / Explosion")]
    public float fuseTime = 3.0f;            // 点火から爆発までの時間（秒）
    public const int blastRadiusTiles = 6;         // タイル単位の爆風半径
    public bool explodeOnStart = false;      // スポーン時に自動で点火するか
    public float explodeRange = 3.0f;        // 爆風範囲（追加拡張用）

    [Header("References")]
    public TileDigging tileDigging;          // TileDigging を Inspector でセット（未設定なら自動検索）
    public GameObject explosionEffect;       // 爆発パーティクル（任意）
    public AudioClip explosionSound;         // 爆発音（任意）
    public LayerMask enemyLayer;             // 敵用（任意）
    public float enemyDamage = 10f;          // 敵へのダメージ（任意）
    public LayerMask rigidbodyLayer;         // 物理オブジェクトへ力を与えるレイヤー（任意）
    public float explosionForce = 300f;      // 一番近い位置での押し出し力（任意）
    public SoundManager SoundManager;

    [Header("Destroy / Player / Jewelry Layers")]
    [Tooltip("爆発で消したい Jewelry を置いているレイヤーをセット")]
    public LayerMask jewelryLayer;           // Jewelry を置いているレイヤー（Inspector で設定）
    [Tooltip("Player を検出するためのレイヤー（Player が属するレイヤーをセット）")]
    public LayerMask playerLayer;            // Player のレイヤー（検出用）

    [Header("Behavior")]
    public bool usePooling = false;          // プール利用時は true にして呼び出し元で戻す
    public float destroyDelay = 0.1f;        // 爆発後にオブジェクトを消す/戻すまでの猶予

    // 内部
    bool isArmed = false;



    void Start()
    {
        // 既にInspectorでセットされていなければ、シーン中の SoundManager を探してセットする
        if (SoundManager == null)
        {
            SoundManager = FindFirstObjectByType<SoundManager>();
            if (SoundManager == null)
                Debug.LogWarning("BombController: SoundManager が見つかりません（シーンに存在しますか？）");
        }

        // TileDigging のフォールバックなど既存処理...
        if (tileDigging == null) tileDigging = FindFirstObjectByType<TileDigging>();
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

    IEnumerator FuseCoroutine()
    {
        // 点火エフェクトや点滅をここで入れられる
        yield return new WaitForSeconds(fuseTime);
        yield return ExplodeAndCleanup();
    }

    IEnumerator ExplodeAndCleanup()
    {
        // 1) エフェクト・音
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
        if (SoundManager != null && explosionSound != null)
        {
            SoundManager.PlaySFX("Sound_Bomb");
        }

        // ワールド半径を先に計算しておく
        float worldRadius = ComputeWorldRadius();

        // 2) TileDigging を使ってタイルを破壊
        if (tileDigging != null && tileDigging.groundTilemap != null)
        {
            // blastRadiusTiles をそのまま使用（必要なら換算ロジックを変えてください）
            tileDigging.DigArea(transform.position, blastRadiusTiles * (int)explodeRange);
        }
        else
        {


            Debug.LogWarning("BombController: TileDigging がアサインされていません。タイル破壊は行われません。");
        }

        // 2.5) --- Jewelry を爆発範囲内からすべて削除する ---
        if (jewelryLayer != 0)
        {
            // 指定レイヤー内のすべての Collider を取得して GameObject を削除
            Collider2D[] jewels = Physics2D.OverlapCircleAll(transform.position, worldRadius, jewelryLayer);
            foreach (var j in jewels)
            {
                if (j != null && j.gameObject != null)
                {
                    // プールを使っている場合は破壊ではなく非アクティブ化する等の処理が必要
                    // ここでは単純に Destroy。ただしプロジェクトにプールがある場合は置き換えてください。
                    Destroy(j.gameObject);
                }
            }
        }

        // 3) （省略している）敵へのダメージ処理など（必要なら有効化）
        // ... (省略)

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

        // 5) 爆発範囲内にプレイヤーがいるかをチェックして何か行いたい場合はここで使える
        bool playerInRange = IsPlayerInRange(); // true/false が得られる（Inspector で playerLayer を設定）
        if (playerInRange)
        {
            // 例: デバッグログ。実際の処理はここに書く（ダメージ、ノックバック、ゲームオーバー判定など）
            Debug.Log("BombController: Player is inside explosion radius!");
        }

        // 6) 後処理（爆弾を破壊 or 非活性化）
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
    /// blastRadiusTiles（タイル数）を基にワールド座標の半径を近似計算して返す
    /// </summary>
    float ComputeWorldRadius()
    {
        if (tileDigging != null && tileDigging.groundTilemap != null)
        {
            Vector3 cellSize = tileDigging.groundTilemap.cellSize;
            float cell = Mathf.Max(cellSize.x, cellSize.y);
            // blastRadiusTiles をそのままセルサイズの倍率として使う
            return blastRadiusTiles * cell;
        }
        return blastRadiusTiles * 1f;
    }

    /// <summary>
    /// 爆発範囲内に Player がいたら true を返す（playerLayer を Inspector で設定）
    /// </summary>
    public bool IsPlayerInRange()
    {
        float worldRadius = ComputeWorldRadius();
        // OverlapCircle を使って playerLayer のいずれかの Collider が存在すれば true
        Collider2D hit = Physics2D.OverlapCircle(transform.position, worldRadius, playerLayer);
        return hit != null;
    }

    /// <summary>
    /// 爆発範囲内の Player をすべて返す（必要なら利用する）
    /// </summary>
    public Collider2D[] GetPlayersInRange()
    {
        float worldRadius = ComputeWorldRadius();
        return Physics2D.OverlapCircleAll(transform.position, worldRadius, playerLayer);
    }

    // Sceneビューで爆風範囲を可視化
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.4f);
        float worldR = (tileDigging != null && tileDigging.groundTilemap != null) ? ComputeWorldRadius() : blastRadiusTiles;
        Gizmos.DrawWireSphere(transform.position, worldR);
    }
}

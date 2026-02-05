using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // 物理&アニメーター
    private Rigidbody2D rb;
    private Animator animator;

    // 攻撃当たり判定(位置)
    public Transform AttackPoint;

    // サウンド（Scene上の SoundManager オブジェクトを Inspector でセット）
    public GameObject SoundManager;

    // 当たり判定(レイヤー)
    public LayerMask StageLayer;
    public LayerMask enemyLayer;
    public LayerMask BlockLayer;

    // 攻撃当たり判定半径
    public float attaackRadius;

    // 移動関係
    private float RunSpeed = 12.0f;
    private float JumpPower = 300.0f;

    // 移動速度補正
    public float speedCorrection = 1.0f;

    //ジャンプ力補正
    public float jumpCorrection = 1.0f;

    public enum MOVE_TYPE { STOP, RIGHT, LEFT }
    public MOVE_TYPE move = MOVE_TYPE.STOP;

    // タイル掘削
    public TileDigging tileDigging;

    //準備段階
    public ShowImage showImage;

    //爆弾コントロール
    public BombController bombController;

    // 掘削範囲（変更可）
    public float digRange = 2.0f;

    // 掘削アニメーション時間（isDig を true にしておく時間）
    [Tooltip("掘削アニメーションを isDig=true にしておく秒数")]
    public float digAnimTime = 0.18f;

    // ボム設置
    public GameObject bombPrefab;
    public int maxBombs = 3;

    // 内部：アニメ coroutine 管理
    Coroutine digAnimCoroutine = null;

    //プレイヤー初期位置
    public Vector3 playerStartPos;

    [Header("Audio Settings")]
    public float digSoundInterval = 0.1f; // 0.1秒間隔以内なら鳴らさない
    private float lastDigSoundTime = 0f;// 最後に掘削音を鳴らした時間

    [Header("Step / Ledge Settings")]
    public float maxStepHeight = 0.4f;      // これ以下の高さ差なら登れる（メートル）
    public float maxDropHeight = 1.0f;      // これ以下の段差ならそのまま落ちる（必要なら調整）
    public float raycastHeight = 1.2f;      // 地面検索を上から行うための高さ（player の身長より少し大きめ）
    public float aheadCheckDistance = 0.3f; // 前方チェックのオフセット（足元からどれだけ先を見るか）
    private float stepCooldown = 0.12f;   // ステップしてから次のステップまでの最短間隔（秒）。調整して下さい。
    private float lastStepTime = -10f;
    public LayerMask groundLayer => StageLayer; // 可読性のための別名

    private BoxCollider2D boxCol; // プレイヤーの当たり判定
    [Header("Step Debug")]
    public bool debugStep = true;

    [Header("Particles")]
    [Tooltip("採掘時に出す 'Mining Particle' のプレハブ（ParticleSystem）をセット")]
    public ParticleSystem MiningParticlePrefab;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();//Rigidbody2D取得
        animator = GetComponent<Animator>();//Animator取得
        playerStartPos = transform.position;//プレイヤー初期位置保存

        // BoxCollider2D 取得（段差処理用）
        boxCol = GetComponent<BoxCollider2D>();
        if (boxCol == null) Debug.LogWarning("PlayerController: BoxCollider2D がありません。段差処理は機能しません。");


    }

    void Update()
    {

        // 準備段階プレイヤー行動許可フラグが立っていたら何もしない
        if (showImage.m_readyPlayerAction) return;
        // ダッシュ準備
        DashPreparation();


        // ジャンプ入力
        if (GroundCheck())
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                animator.SetBool("isJump", true);
                Jump();
            }
        }
        else
        {
            animator.SetBool("isJump", false);
        }

        // 掘削 / 攻撃：マウス左ボタンをホールドで連続掘削（attackCooldown で制御）
        if (Input.GetMouseButton(0))
        {
            // Dig() は壊したタイル数を返すようにする
            int removed = Dig();

            // タイルが一つでも壊れたら効果音を鳴らす
            if (removed > 0)
            {
                // 音の重複による音割れ防止
                if (SoundManager != null && Time.time - lastDigSoundTime > digSoundInterval)
                {
                    var sm = SoundManager.GetComponent<SoundManager>();
                    if (sm != null)
                    {
                        // ピッチを変更して再生する
                        float randomPitch = Random.Range(0.8f, 1.2f);
                        sm.PlaySFX("Sound_Dig", 1.0f, randomPitch);

                        lastDigSoundTime = Time.time; // 再生時間を更新

                        if (MiningParticlePrefab != null)
                        {
                            // ParticleSystem プレハブを生成して再生
                            ParticleSystem ps = Instantiate(MiningParticlePrefab, transform.position, Quaternion.identity);
                            var main = ps.main;
                            ps.Play();

                            // 安全に破棄するために寿命を計算して Destroy（duration + startLifetime の最大値）
                            float lifetime = main.duration;
                            // startLifetime は MinMaxCurve なので constantMax を使う（幅がある場合に最大を取る）
                            lifetime += main.startLifetime.constantMax;
                            Destroy(ps.gameObject, lifetime + 0.1f); // 余裕を少し追加
                        }

                    }
                }
            }

            // アニメーション：掘削中フラグを短時間 true にする
            if (digAnimCoroutine != null) StopCoroutine(digAnimCoroutine);
            digAnimCoroutine = StartCoroutine(DoDigAnimCoroutine(digAnimTime));
        }

        // ボム設置
        if (Input.GetKeyDown(KeyCode.B) && maxBombs > 0)
        {
            PlaceBomb();
        }

    }

    private void FixedUpdate()
    {
        //ダッシュ処理
        Dash();


        // 段差処理関数呼び出し
        TryStepUpOrDown();
    }

    // ダッシュ準備（入力取得）
    private void DashPreparation()
    {
        // 横移動入力取得
        float horizonkey = Input.GetAxis("Horizontal");
        if (horizonkey == 0)
        {
            // 停止
            move = MOVE_TYPE.STOP;
            animator.SetBool("isWalk", false);//歩行アニメーション停止
        }
        else if (horizonkey > 0)
        {
            // 右移動
            move = MOVE_TYPE.RIGHT;
            animator.SetBool("isWalk", true);//歩行アニメーション開始
        }
        else if (horizonkey < 0)
        {
            // 左移動
            move = MOVE_TYPE.LEFT;
            animator.SetBool("isWalk", true);//歩行アニメーション開始
        }
    }

    // ダッシュ（FixedUpdate）
    private void Dash()
    {
        // 横移動
        Vector3 scale = transform.localScale;
        if (move == MOVE_TYPE.STOP) RunSpeed = 0;//停止
        else if (move == MOVE_TYPE.RIGHT) { scale.x = 1; RunSpeed = 12; }//右移動
        else if (move == MOVE_TYPE.LEFT) { scale.x = -1; RunSpeed = -12; }//左移動

        // 向き変更と速度設定
        transform.localScale = scale;
        rb.velocity = new Vector2(RunSpeed * speedCorrection, rb.velocity.y);
    }

    // ジャンプ
    private void Jump()
    {
        rb.AddForce(Vector2.up * JumpPower * jumpCorrection);
    }

    /// <summary>
    /// 採掘処理：Tile破壊（範囲）を行い、壊したタイル数を返す
    /// </summary>
    private int Dig()
    {
        // 敵への当たり判定
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(AttackPoint.position, attaackRadius, enemyLayer);
        foreach (var e in hitEnemies)
        {
            Debug.Log("Hit enemy: " + e.name);
            // 敵にダメージを与える処理をここへ
        }

        // タイル破壊（AttackPoint を中心に brush 半径）
        if (tileDigging != null)
        {
            Vector3 attackPos = AttackPoint.position;
            int removed = tileDigging.DigArea(attackPos, Mathf.Max(0, (int)digRange));
            return removed;
        }
        return 0;
    }

    // 掘削アニメーションのコルーチン（短時間 isDig=true にする）
    IEnumerator DoDigAnimCoroutine(float duration)
    {
        animator.SetBool("isDig", true);
        yield return new WaitForSeconds(duration);
        animator.SetBool("isDig", false);
        digAnimCoroutine = null;
    }

    // ギズモ表示：攻撃範囲
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (AttackPoint != null) Gizmos.DrawWireSphere(AttackPoint.position, attaackRadius);
    }

    // 地面接地判定
    private bool GroundCheck()
    {
        Vector3 startposition = transform.position;
        Vector3 endposition = transform.position - transform.up * 4.0f;
        Debug.DrawLine(startposition, endposition, Color.red);
        return Physics2D.Linecast(startposition, endposition, StageLayer);
    }

    // スピード取得
    public float GetSpeed()
    {
        return RunSpeed;
    }

    // ボム設置処理
    void PlaceBomb()
    {
        Vector3 pos = transform.position;
        GameObject b = Instantiate(bombPrefab, pos, Quaternion.identity);
        b.GetComponent<BombController>().Ignite();
        maxBombs--;
    }

    public void OnBombHit()
    {
        // ボムに当たったときの処理
        ScoreManagerSingleton.instance.m_score /= 2;//スコア半減
        transform.position = playerStartPos;//プレイヤー初期位置に戻す
    }

    // --- 段差処理関数 ---
    void TryStepUpOrDown()
    {
        if (move == MOVE_TYPE.STOP) return;
        if (boxCol == null) return;

        // クールダウン中は処理しない
        if (Time.time - lastStepTime < stepCooldown) return;

        float dir = (move == MOVE_TYPE.RIGHT) ? 1f : -1f;
        Bounds b = boxCol.bounds;

        // --- 1) レイのスタートYはプレイヤー基準にする（穴の深さで大きくブレない） ---
        float rayStartY = transform.position.y + raycastHeight; // プレイヤーの中心を基準
        float rayMaxDistance = raycastHeight + 4.0f; // 十分な長さ

        // --- 2) 現在地の地面を取得（プレイヤー基準で上から落とす） ---
        Vector2 rayOriginCurrent = new Vector2(transform.position.x, rayStartY);
        RaycastHit2D hitCurrent = Physics2D.Raycast(rayOriginCurrent, Vector2.down, rayMaxDistance, groundLayer);

        if (debugStep)
        {
            Debug.DrawLine(rayOriginCurrent, rayOriginCurrent + Vector2.down * rayMaxDistance, Color.cyan);
        }

        if (hitCurrent.collider == null)
        {
            // 足元に地面がない（空中・崖）なら段差処理しない
            return;
        }

        // 小さな安全マージン分上向きの面を拾わせる
        float currentGroundY = hitCurrent.point.y;

        // --- 3) 前方の障害物（壁）検出：BoxCast の始点を"足元"（プレイヤーの底）基準にする ---
        float footY = transform.position.y - b.extents.y + 0.02f;
        Vector2 footOrigin = new Vector2(transform.position.x + dir * 0.05f, footY); // 少し前にずらして開始
        Vector2 boxSize = new Vector2(b.size.x * 0.8f, 0.18f);
        float boxDistance = b.extents.x + aheadCheckDistance;

        RaycastHit2D frontHit = Physics2D.BoxCast(footOrigin, boxSize, 0f, Vector2.right * dir, boxDistance, groundLayer);

        if (debugStep)
        {
            Vector2 castFrom = footOrigin;
            Vector2 castTo = footOrigin + Vector2.right * dir * boxDistance;
            Debug.DrawLine(castFrom, castTo, frontHit.collider != null ? Color.red : Color.gray, 0.05f);
        }

        if (frontHit.collider == null)
        {
            // 前方に本当に障害物が無ければ何もしない
            return;
        }

        // --- 4) 前方地面を調べる（始点は currentGroundY + raycastHeight：現在地の地面基準で上から落とす） ---
        float aheadRayX = transform.position.x + dir * (b.extents.x + aheadCheckDistance);
        float aheadRayStartY = currentGroundY + raycastHeight;
        float aheadRayDistance = raycastHeight + 3.0f;
        Vector2 rayOriginAhead = new Vector2(aheadRayX, aheadRayStartY);
        RaycastHit2D hitAhead = Physics2D.Raycast(rayOriginAhead, Vector2.down, aheadRayDistance, groundLayer);

        if (debugStep)
        {
            Debug.DrawLine(rayOriginAhead, rayOriginAhead + Vector2.down * aheadRayDistance, Color.yellow, 0.05f);
        }

        if (hitAhead.collider == null)
        {
            // 前方に地面がない（崖など）は処理しない
            return;
        }

        // --- 5) 追加フィルタ：拾ったヒットが「その位置の真正面の地面か」を簡易確認 ---
        // ・法線が上向きか（ほぼ水平面）
        // ・ヒットした x が期待位置に近いか（遠くの斜面を拾わないため）
        if (Vector2.Dot(hitAhead.normal, Vector2.up) < 0.5f)
        {
            if (debugStep) Debug.Log("Ignored hitAhead: normal not up.");
            return;
        }
        float horizontalDiff = Mathf.Abs(hitAhead.point.x - aheadRayX);
        float acceptXThreshold = Mathf.Max(0.6f, b.extents.x + 0.1f); // タイル幅に合わせて調整
        if (horizontalDiff > acceptXThreshold)
        {
            if (debugStep) Debug.Log($"Ignored hitAhead: hit.x too far (diff={horizontalDiff:F2}).");
            return;
        }

        float aheadGroundY = hitAhead.point.y;
        float dy = aheadGroundY - currentGroundY;

        if (debugStep) Debug.Log($"StepCheck: curY={currentGroundY:F2}, aheadY={aheadGroundY:F2}, dy={dy:F2}");

        // --- 6) 条件に合えばスナップ（上向きで maxStepHeight 以下） ---
        if (dy > 0f && dy <= maxStepHeight)
        {
            // 移動先（プレイヤーの現在位置を上方に dy）に当たりがないか
            Vector2 targetCenter = new Vector2(transform.position.x, transform.position.y + dy);
            Vector2 overlapSize = new Vector2(b.size.x * 0.9f, b.size.y * 0.9f);
            Collider2D overlap = Physics2D.OverlapBox(targetCenter, overlapSize, 0f, groundLayer);

            if (debugStep)
            {
                Vector2 tl = new Vector2(targetCenter.x - overlapSize.x / 2, targetCenter.y + overlapSize.y / 2);
                Vector2 br = new Vector2(targetCenter.x + overlapSize.x / 2, targetCenter.y - overlapSize.y / 2);
                Debug.DrawLine(tl, new Vector2(br.x, tl.y), Color.green, 0.05f);
                Debug.DrawLine(tl, new Vector2(tl.x, br.y), Color.green, 0.05f);
                Debug.DrawLine(br, new Vector2(tl.x, br.y), Color.green, 0.05f);
                Debug.DrawLine(br, new Vector2(br.x, tl.y), Color.green, 0.05f);
            }

            if (overlap == null)
            {
                // 一度だけスナップ
                Vector2 newPos = new Vector2(transform.position.x, transform.position.y + dy);
                rb.MovePosition(newPos);
                rb.velocity = new Vector2(rb.velocity.x, 0f);
                lastStepTime = Time.time;
            }
            else
            {
                if (debugStep) Debug.Log("Step blocked: no head clearance.");
            }
        }
    }

    
}

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

    public enum MOVE_TYPE { STOP, RIGHT, LEFT }
    public MOVE_TYPE move = MOVE_TYPE.STOP;

    public TileDigging tileDigging;

    // 攻撃のクールダウン
    public float attackCooldown = 0.25f; // 連打間隔（秒）
    private float attackTimer = 0f;

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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        // ダッシュ準備
        DashPreparation();

        // 攻撃準備（アニメ系の事前処理があれば）
        AttackPreparation();

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
            attackTimer = attackCooldown;

            // Attack() は壊したタイル数を返すようにする
            int removed = Attack();

            // タイルが一つでも壊れたら効果音を鳴らす
            if (removed > 0)
            {
                if (SoundManager != null)
                {
                    var sm = SoundManager.GetComponent<SoundManager>();
                    if (sm != null) sm.PlaySFX("Sound_Dig");
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

        // ガード
        Guard();
    }

    private void FixedUpdate()
    {
        Dash();
    }

    // ダッシュ準備（入力取得）
    private void DashPreparation()
    {
        float horizonkey = Input.GetAxis("Horizontal");
        if (horizonkey == 0)
        {
            move = MOVE_TYPE.STOP;
            animator.SetBool("isWalk", false);
        }
        else if (horizonkey > 0)
        {
            move = MOVE_TYPE.RIGHT;
            animator.SetBool("isWalk", true);
        }
        else if (horizonkey < 0)
        {
            move = MOVE_TYPE.LEFT;
            animator.SetBool("isWalk", true);
        }
    }

    // ダッシュ（FixedUpdate）
    private void Dash()
    {
        Vector3 scale = transform.localScale;
        if (move == MOVE_TYPE.STOP) RunSpeed = 0;
        else if (move == MOVE_TYPE.RIGHT) { scale.x = 1; RunSpeed = 12; }
        else if (move == MOVE_TYPE.LEFT) { scale.x = -1; RunSpeed = -12; }

        transform.localScale = scale;
        rb.velocity = new Vector2(RunSpeed * speedCorrection, rb.velocity.y);
    }

    private void Jump()
    {
        rb.AddForce(Vector2.up * JumpPower);
    }

    private void AttackPreparation()
    {
        // アニメーションや攻撃チャージ等を入れる場所（今は未使用）
    }

    /// <summary>
    /// 攻撃処理：敵ヒット判定（既存）＋Tile破壊（範囲）を行い、壊したタイル数を返す
    /// </summary>
    private int Attack()
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

    private void Guard()
    {
        if (Input.GetKey(KeyCode.V)) this.tag = "Guard";
        else this.tag = "Player";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (AttackPoint != null) Gizmos.DrawWireSphere(AttackPoint.position, attaackRadius);
    }

    private bool GroundCheck()
    {
        Vector3 startposition = transform.position;
        Vector3 endposition = transform.position - transform.up * 4.0f;
        Debug.DrawLine(startposition, endposition, Color.red);
        return Physics2D.Linecast(startposition, endposition, StageLayer);
    }

    public float GetSpeed()
    {
        return RunSpeed;
    }

    void PlaceBomb()
    {
        Vector3 pos = transform.position;
        GameObject b = Instantiate(bombPrefab, pos, Quaternion.identity);
        b.GetComponent<BombController>().Ignite();
        maxBombs--;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            // 着地時処理（必要なら）
        }
    }
}

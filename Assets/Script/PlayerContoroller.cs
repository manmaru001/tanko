using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //物理&アニメーター
    private Rigidbody2D rb;
    //private Animator //animator;

    //攻撃当たり判定(位置)
    public Transform AttackPoint;

    //当たり判定(レイヤー)
    public LayerMask StageLayer;
    public LayerMask enemyLayer;
    public LayerMask BlockLayer;

    //エネミーオブジェクト
    [SerializeField] private GameObject enemy = null;

    //攻撃当たり判定半径
    public float attaackRadius;

    //移動関係
    private float RunSpeed = 3.0f;
    private float JumpPower = 300.0f;
    private float PushSpeed = 3.0f;

    public enum MOVE_TYPE
    {
        STOP,
        RIGHT,
        LEFT,
    }
    public MOVE_TYPE move = MOVE_TYPE.STOP;//初期状態は停止させる

    // Start is called before the first frame update
    void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        enemy = GameObject.Find("Enemy");

        //ダッシュ準備
        DashPreparation();

        //攻撃準備
        AttackPreparation();

        //ジャンプ
        if (GroundCheck())
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                //animator.SetBool("Jump", true);
                //Jump();
            }
        }

        //攻撃
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //animator.SetBool("Attack", true);
            //Attack();
        }

        //ガード
        Guard();
        //Debug.Log(this.tag);

    }
    private void FixedUpdate()
    {
        //ダッシュ
        Dash();
    }

    //ダッシュ準備
    private void DashPreparation()
    {
        float horizonkey = Input.GetAxis("Horizontal");

        // 取得した水平方向のキーを元に分岐
        if (horizonkey == 0)
        {
            // キー入力なしの場合は停止
            move = MOVE_TYPE.STOP;
            //animator.SetBool("Dash", false);
        }
        else if (horizonkey > 0)
        {
            // キー入力が正の場合は右へ移動する
            move = MOVE_TYPE.RIGHT;
            //animator.SetBool("Dash", true);
        }
        else if (horizonkey < 0)
        {
            // キー入力が負の場合は左へ移動する
            move = MOVE_TYPE.LEFT;
            //animator.SetBool("Dash", true);
        }
    }

    //ダッシュ
    private void Dash()
    {
        // Playerの方向を決めるためにスケールの取り出し
        Vector3 scale = transform.localScale;
        if (move == MOVE_TYPE.STOP)
        {
            RunSpeed = 0;

        }
        else if (move == MOVE_TYPE.RIGHT)
        {
            scale.x = 1; // 右向き
            RunSpeed = 3;

        }
        else if (move == MOVE_TYPE.LEFT)
        {
            scale.x = -1; // 左向き
            RunSpeed = -3;

        }
        transform.localScale = scale; // scaleを代入
                                      // rigidbody2Dのvelocity(速度)へ取得したRunSpeedを入れる。y方向は動かないのでそのままにする
        rb.velocity = new Vector2(RunSpeed, rb.velocity.y);
    }

    //ジャンプ
    private void Jump()
    {
        float v = Input.GetAxisRaw("Vertical");

        //if (!jumpArrow)
        //{
        rb.AddForce(Vector2.up * JumpPower);
        //}
    }

    //攻撃準備
    private void AttackPreparation()
    {
        ////アニメーションの進行状況をチェック
        //if (//animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Attack") &&
        //        //animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f)
        //{
        //    //animator.SetBool("Attack", false);
        //}
    }


    //防御
    private void Guard()
    {
        if (Input.GetKey(KeyCode.V))
        {
            this.tag = "Guard";
        }
        else
        {
            this.tag = "Player";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AttackPoint.position, attaackRadius);
    }

    //地面との接触
    private bool GroundCheck()
    {
        Vector3 startposition = transform.position;
        Vector3 endposition = transform.position - transform.up * 0.6f;

        Debug.DrawLine(startposition, endposition, Color.red);

        return Physics2D.Linecast(startposition, endposition, StageLayer);
    }

    //速度の取得
    public float GetSpeed()
    {
        return RunSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ground")
        {
            //animator.SetBool("Jump", false);
        }
    }
}


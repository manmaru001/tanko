using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimMouseController : MonoBehaviour
{
    [Header("References")]
    public Transform AttackPoint;        // 狙いポイント（Inspectorで指定）
    public Camera mainCamera;            // カメラ（nullなら Camera.main を使用）

    [Header("Radius")]
    public float attackPointRadius = 1.0f;
    public float minRadius = 0.2f;
    public float maxRadius = 3.0f;
    public float radiusAdjustSpeed = 1.0f;

    [Header("Smoothing")]
    public bool smoothAiming = true;
    public float aimSmoothSpeed = 15f;

    [Header("Angle Limits (degrees, relative to player forward)")]
    [Tooltip("プレイヤーの前方向（facing が右なら右方向）を0度として、この下限を設定")]
    public float minAngle = -135f;   // 左側の限界（負）
    [Tooltip("プレイヤーの前方向（facing が右なら右方向）を0度として、この上限を設定")]
    public float maxAngle = 135f;    // 右側の限界（正）

    [Header("Flip / Clamp Behavior")]
    [Tooltip("プレイヤーが左右反転したときに clamp 範囲を自動で反転するか")]
    public bool invertClampWhenFlipped = true;

    [Tooltip("clamp 範囲をこの角度だけ両側に拡張する（例: 10 => min -=10, max +=10）")]
    public float extraClamp = 0f;

    [Header("Soft Clamp (allow aiming beyond limits but pull toward allowed range)")]
    [Tooltip("softClamp=true のとき、softness が 1 でハードクランプ、0 で完全自由（clamp 無効）")]
    public bool softClamp = false;
    [Range(0f, 1f)]
    public float softness = 1.0f;

    [Header("Keyboard Fine Control (optional)")]
    public bool enableKeyboardRotate = true;
    public float keyboardRotateSpeed = 120f;

    // 向き取得方法を選べるよう列挙型を用意
    public enum FacingSource { LocalScale, SpriteFlipX, Velocity }
    [Header("Facing Detection")]
    [Tooltip("プレイヤーの向きをどの方法で判定するか")]
    public FacingSource facingSource = FacingSource.LocalScale;
    [Tooltip("facingSource が SpriteFlipX のときに指定する SpriteRenderer")]
    public SpriteRenderer optionalSpriteRenderer; // Sprite.flipX を使う場合にセット
    [Tooltip("facingSource が Velocity のときに指定する Rigidbody2D")]
    public Rigidbody2D optionalRbForVelocity;     // 速度で向きを取る場合にセット

    // 内部状態
    Vector3 currentAttackPointPos;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // AttackPoint が未設定なら自動生成してプレイヤーの子にする
        if (AttackPoint == null)
        {
            GameObject go = new GameObject("AttackPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.right * Mathf.Clamp(attackPointRadius, minRadius, maxRadius);
            AttackPoint = go.transform;
            Debug.LogWarning("AimMouseController: AttackPoint が未設定だったため自動生成しました。");
        }

        attackPointRadius = Mathf.Clamp(attackPointRadius, minRadius, maxRadius);
        currentAttackPointPos = AttackPoint.position;
    }

    void Update()
    {
        // マウスに合わせて攻撃点を更新（角度制限あり）
        UpdateAimWithMouseWithAngleLimit();

        // キーボードで微調整（オプション）
        OptionalKeyboardRotate();
    }

    // マウスホイールで半径を変えたい場合はこの関数を有効化して Update() で呼んでください
    void UpdateRadiusByWheel()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0001f)
        {
            attackPointRadius += wheel * radiusAdjustSpeed;
            attackPointRadius = Mathf.Clamp(attackPointRadius, minRadius, maxRadius);
        }
    }

    // プレイヤーの向きを +1 (右) / -1 (左) で返すヘルパー
    int GetFacingSign()
    {
        switch (facingSource)
        {
            case FacingSource.SpriteFlipX:
                if (optionalSpriteRenderer != null)
                {
                    // flipX = true のときスプライトは左右反転しているので向きは左
                    return optionalSpriteRenderer.flipX ? -1 : 1;
                }
                // if null, fallthrough to LocalScale
                goto case FacingSource.LocalScale;

            case FacingSource.Velocity:
                if (optionalRbForVelocity != null)
                {
                    if (optionalRbForVelocity.velocity.x > 0.01f) return 1;
                    if (optionalRbForVelocity.velocity.x < -0.01f) return -1;
                    // 速度がほぼ 0 のときは localScale 側にフォールバック
                }
                goto case FacingSource.LocalScale;

            case FacingSource.LocalScale:
            default:
                // localScale.x >= 0 => 右向き（1）、負なら左向き（-1）
                return transform.localScale.x >= 0f ? 1 : -1;
        }
    }

    // マウス位置に向かわせつつ、プレイヤーの向きを基準に相対角度を制限する
    void UpdateAimWithMouseWithAngleLimit()
    {
        if (AttackPoint == null || mainCamera == null) return;

        // マウスのスクリーン座標をワールド座標に変換
        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;

        // プレイヤー中心からマウスへのベクトルと角度を算出
        Vector3 toMouse = mouseWorld - transform.position;
        if (toMouse.sqrMagnitude < 1e-6f) toMouse = transform.right; // ゼロ除算対策
        float mouseAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;

        // 向きを明示的に取得（+1:右, -1:左）
        int facingSign = GetFacingSign();

        // forwardAngle を向きに応じて決める（右向きなら 0°, 左向きなら 180°）
        float forwardAngle = (facingSign > 0) ? 0f : 180f;

        // マウス角度を forwardAngle 基準の相対角度に変換（-180..180）
        float relativeAngle = Mathf.DeltaAngle(forwardAngle, mouseAngle);

        // clamp の有効範囲を計算（左右反転オプションと extraClamp を考慮）
        float effMin = minAngle;
        float effMax = maxAngle;

        // invertClampWhenFlipped は互換性のため残すが、GetFacingSign で向きを取得しているため
        // transform.localScale と一致しない場合でも facingSign に従って動作します。
        if (invertClampWhenFlipped && facingSign < 0)
        {
            // 左向きのときに範囲を反転する（従来の挙動を維持）
            effMin = -maxAngle;
            effMax = -minAngle;
        }

        // 範囲を拡張したい場合
        effMin -= extraClamp;
        effMax += extraClamp;

        // softClamp が有効なら滑らかに制限、無効ならハードクランプ
        float clampedRel;
        if (softClamp)
        {
            float hardClamped = Mathf.Clamp(relativeAngle, effMin, effMax);
            // softness==1 -> 完全ハードクランプ、softness==0 -> 制限無効（relativeAngleのまま）
            clampedRel = Mathf.Lerp(relativeAngle, hardClamped, Mathf.Clamp01(softness));
        }
        else
        {
            clampedRel = Mathf.Clamp(relativeAngle, effMin, effMax);
        }

        // 実際の世界角 = forwardAngle + clampedRel
        float newAngle = forwardAngle + clampedRel;
        float rad = newAngle * Mathf.Deg2Rad;
        Vector3 newDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        // 目標位置（プレイヤー中心 + 方向 * 半径）
        Vector3 targetPos = transform.position + newDir.normalized * attackPointRadius;

        // スムースに追従 or 即時反映
        if (smoothAiming)
            currentAttackPointPos = Vector3.Lerp(currentAttackPointPos, targetPos, Time.deltaTime * aimSmoothSpeed);
        else
            currentAttackPointPos = targetPos;

        AttackPoint.position = currentAttackPointPos;
    }

    // キーボードで回転を微調整する（Optional）
    void OptionalKeyboardRotate()
    {
        if (!enableKeyboardRotate) return;

        float h = Input.GetAxis("Horizontal");
        if (Mathf.Abs(h) > 0.01f)
        {
            Vector3 dir = (currentAttackPointPos - transform.position);
            float currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float newAngle = currentAngle + h * keyboardRotateSpeed * Time.deltaTime;

            int facingSign = GetFacingSign();
            float forwardAngle = (facingSign > 0) ? 0f : 180f;

            float effMin = minAngle;
            float effMax = maxAngle;
            if (invertClampWhenFlipped && facingSign < 0)
            {
                effMin = -maxAngle;
                effMax = -minAngle;
            }
            effMin -= extraClamp;
            effMax += extraClamp;

            float rel = Mathf.DeltaAngle(forwardAngle, newAngle);
            float clampedRel;
            if (softClamp)
            {
                float hardClamped = Mathf.Clamp(rel, effMin, effMax);
                clampedRel = Mathf.Lerp(rel, hardClamped, Mathf.Clamp01(softness));
            }
            else
            {
                clampedRel = Mathf.Clamp(rel, effMin, effMax);
            }

            float finalAngle = forwardAngle + clampedRel;
            float rad = finalAngle * Mathf.Deg2Rad;
            Vector3 finalDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 newPos = transform.position + finalDir.normalized * attackPointRadius;

            currentAttackPointPos = newPos;
            AttackPoint.position = currentAttackPointPos;
        }
    }

    // Gizmo 表示（シーンビューで範囲が見える）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackPointRadius);

        if (AttackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, AttackPoint.position);
            Gizmos.DrawSphere(AttackPoint.position, 0.05f);
        }

#if UNITY_EDITOR
        // 許容角を可視化（左/右反転を考慮）
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.12f);

        // 基準角は向きによって 0deg / 180deg にする
        int facingSign = 1;
        if (Application.isPlaying)
        {
            facingSign = GetFacingSign();
        }
        float baseAngle = (facingSign > 0) ? 0f : 180f;

        float effMin = minAngle;
        float effMax = maxAngle;
        if (invertClampWhenFlipped && facingSign < 0)
        {
            effMin = -maxAngle;
            effMax = -minAngle;
        }
        effMin -= extraClamp;
        effMax += extraClamp;

        float startAngle = (baseAngle + effMin) * Mathf.Deg2Rad;
        float sweep = effMax - effMin;
        Vector3 startDir = new Vector3(Mathf.Cos(startAngle), Mathf.Sin(startAngle), 0f);
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.forward, startDir, sweep, attackPointRadius);
#endif
    }
}

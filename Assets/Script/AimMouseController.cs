using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimMouseController : MonoBehaviour
{
    [Header("References")]
    public Transform AttackPoint;
    public Camera mainCamera;

    [Header("Radius")]
    public float attackPointRadius = 100.8f;
    public float minRadius = 0.2f;
    public float maxRadius = 3.0f;
    public float radiusAdjustSpeed = 1.0f;

    [Header("Smoothing")]
    public bool smoothAiming = true;
    public float aimSmoothSpeed = 15f;

    [Header("Angle Limits (degrees, relative to player forward)")]
    [Tooltip("プレイヤーの前方向（transform.right）を0度として、この下限を設定")]
    public float minAngle = -135f;   // 左側の限界（負）
    [Tooltip("プレイヤーの前方向（transform.right）を0度として、この上限を設定")]
    public float maxAngle = 135f;    // 右側の限界（正）

    [Header("Keyboard Fine Control (optional)")]
    public bool enableKeyboardRotate = true;
    public float keyboardRotateSpeed = 120f;

    Vector3 currentAttackPointPos;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (AttackPoint == null)
        {
            GameObject go = new GameObject("AttackPoint");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.right * attackPointRadius;
            AttackPoint = go.transform;
            Debug.LogWarning("AimWithMouse: AttackPoint was not set. Auto-created a child AttackPoint.");
        }

        attackPointRadius = Mathf.Clamp(attackPointRadius, minRadius, maxRadius);
        currentAttackPointPos = AttackPoint.position;
    }

    void Update()
    {
        //UpdateRadiusByWheel();
        UpdateAimWithMouseWithAngleLimit();
        OptionalKeyboardRotate();
    }

    void UpdateRadiusByWheel()
    {
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(wheel) > 0.0001f)
        {
            attackPointRadius += wheel * radiusAdjustSpeed;
            attackPointRadius = Mathf.Clamp(attackPointRadius, minRadius, maxRadius);
        }
    }

    // マウス位置に向かわせつつ、プレイヤー前方を 0° として相対角度を min/max で制限
    void UpdateAimWithMouseWithAngleLimit()
    {
        if (AttackPoint == null || mainCamera == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;

        // マウスからの角度（ワールド座標、-180..180）
        Vector3 toMouse = mouseWorld - transform.position;
        if (toMouse.sqrMagnitude < 1e-6f) toMouse = transform.right; // ゼロ除算対策
        float mouseAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;

        // プレイヤーの"前"ベクトル（transform.right）を基準角度に
        Vector3 forward = transform.right; // 右向きが 0°
        float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        // 相対角度（-180..180） = mouseAngle - forwardAngle, normalized
        float relativeAngle = Mathf.DeltaAngle(forwardAngle, mouseAngle);

        // clamp 相対角度（Inspector の minAngle,maxAngle）
        float clampedRel = Mathf.Clamp(relativeAngle, minAngle, maxAngle);

        // 新しい角度 = 基準角 + clampedRel
        float newAngle = forwardAngle + clampedRel;
        float rad = newAngle * Mathf.Deg2Rad;
        Vector3 newDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);

        Vector3 targetPos = transform.position + newDir.normalized * attackPointRadius;

        if (smoothAiming)
            currentAttackPointPos = Vector3.Lerp(currentAttackPointPos, targetPos, Time.deltaTime * aimSmoothSpeed);
        else
            currentAttackPointPos = targetPos;

        AttackPoint.position = currentAttackPointPos;
        // AttackPoint.up = newDir; // 見た目スプライトを向けたいなら有効化
    }

    void OptionalKeyboardRotate()
    {
        if (!enableKeyboardRotate) return;

        float h = Input.GetAxis("Horizontal");
        if (Mathf.Abs(h) > 0.01f)
        {
            // 現在の角度（ワールド）を取得
            Vector3 dir = (currentAttackPointPos - transform.position);
            float currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float newAngle = currentAngle + h * keyboardRotateSpeed * Time.deltaTime;

            // ここでも相対角度に従って clamp する（プレイヤー前方基準）
            Vector3 forward = transform.right;
            float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float rel = Mathf.DeltaAngle(forwardAngle, newAngle);
            float clampedRel = Mathf.Clamp(rel, minAngle, maxAngle);
            float finalAngle = forwardAngle + clampedRel;

            float rad = finalAngle * Mathf.Deg2Rad;
            Vector3 finalDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 newPos = transform.position + finalDir.normalized * attackPointRadius;

            currentAttackPointPos = newPos;
            AttackPoint.position = currentAttackPointPos;
        }
    }

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

        // 可視化：許容角（Gizmo で簡易的に表示）
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.1f);
        Vector3 fwd = transform.right;
        float baseAngle = Mathf.Atan2(fwd.y, fwd.x) * Mathf.Rad2Deg;
        // draw two rays for min/max angle
        float a1 = (baseAngle + minAngle) * Mathf.Deg2Rad;
        float a2 = (baseAngle + maxAngle) * Mathf.Deg2Rad;
        Vector3 p1 = transform.position + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * attackPointRadius;
        Vector3 p2 = transform.position + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0f) * attackPointRadius;
        UnityEditor.Handles.DrawSolidArc(transform.position, Vector3.forward, (p1 - transform.position), maxAngle - minAngle, attackPointRadius);
#endif
    }
}

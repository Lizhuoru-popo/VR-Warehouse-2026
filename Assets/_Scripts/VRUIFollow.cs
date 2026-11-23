using UnityEngine;

/// <summary>
/// VR UI跟随脚本 - 使UI始终在玩家视角中
/// 
/// 两种用法：
/// 1. 按钮跟随（continuousFollow=true）：持续跟随玩家视角，像HUD一样
/// 2. 面板定位（continuousFollow=false, snapOnEnable=true）：激活时一次性定位到玩家面前
/// </summary>
public class VRUIFollow : MonoBehaviour
{
    [Header("跟随目标（默认主相机）")]
    public Transform target;

    [Header("位置设置")]
    public float distance = 2.5f;
    [Tooltip("高度偏移，负数=低于视线")]
    public float heightOffset = 0f;
    [Tooltip("左右偏移，正数=右侧")]
    public float sideOffset = 0f;

    [Header("跟随模式")]
    [Tooltip("持续跟随玩家视角（按钮用）")]
    public bool continuousFollow = true;
    [Tooltip("激活时立即定位到玩家面前（面板用）")]
    public bool snapOnEnable = false;

    [Header("平滑设置")]
    public bool smoothFollow = true;
    public float smoothSpeed = 10f;

    void Start()
    {
        if (target == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                target = cam.transform;
        }

        if (continuousFollow)
            SnapToTarget();
    }

    void OnEnable()
    {
        if (snapOnEnable)
            SnapToTarget();
    }

    void LateUpdate()
    {
        if (!continuousFollow) return;
        if (target == null) return;

        Vector3 targetPos = CalculateTargetPosition();
        Quaternion targetRot = CalculateTargetRotation(targetPos);

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
    }

    /// <summary>
    /// 立即对齐到目标位置
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                target = cam.transform;
        }
        if (target == null) return;

        transform.position = CalculateTargetPosition();
        transform.rotation = CalculateTargetRotation(transform.position);
    }

    Vector3 CalculateTargetPosition()
    {
        // 只取yaw方向，不跟pitch
        Vector3 forward = target.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = target.right;
        right.y = 0f;
        right.Normalize();

        Vector3 pos = target.position + forward * distance + right * sideOffset;
        pos.y = target.position.y + heightOffset;
        return pos;
    }

    Quaternion CalculateTargetRotation(Vector3 currentPos)
    {
        // UI 正面朝向玩家（让 -Z 轴指向玩家）
        // LookRotation(forward) 让 +Z 指向 forward，所以 forward 要取远离玩家的方向
        Vector3 forward = currentPos - target.position;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.01f)
            return Quaternion.LookRotation(forward);
        return transform.rotation;
    }
}

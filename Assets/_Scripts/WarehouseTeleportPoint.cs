using UnityEngine;

/// <summary>
/// 仓库传送点 - 挂在场景中的空物体上，标记玩家进入该仓库时的出生位置和朝向
/// 
/// 使用方法：
/// 1. 在场景中创建空物体，命名为 TeleportPoint_仓库名（如 TeleportPoint_apple）
/// 2. 将物体移动到你想要玩家出现的位置，旋转到你想要玩家面向的方向
/// 3. 添加本脚本，在 warehouseName 中填入仓库标识（apple / frozen_meat / chocolate / main_menu）
/// 4. 运行时会自动注册到 WarehouseGameManager
/// </summary>
public class WarehouseTeleportPoint : MonoBehaviour
{
    [Header("仓库标识")]
    [Tooltip("apple / frozen_meat / chocolate / main_menu")]
    public string warehouseName = "apple";

    [Header("是否显示Gizmo")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.cyan;
    public float gizmoRadius = 0.5f;

    void Start()
    {
        if (WarehouseGameManager.Instance != null)
        {
            WarehouseGameManager.Instance.RegisterTeleportPoint(warehouseName, transform);
        }
        else
        {
            // 如果GameManager还没创建，延迟一帧注册
            StartCoroutine(RegisterDelayed());
        }
    }

    System.Collections.IEnumerator RegisterDelayed()
    {
        yield return null; // 等一帧

        if (WarehouseGameManager.Instance != null)
        {
            WarehouseGameManager.Instance.RegisterTeleportPoint(warehouseName, transform);
        }
        else
        {
            Debug.LogWarning($"[TeleportPoint] 无法注册传送点 {warehouseName}，WarehouseGameManager 不存在!");
        }
    }

    void OnDestroy()
    {
        if (WarehouseGameManager.Instance != null)
        {
            WarehouseGameManager.Instance.UnregisterTeleportPoint(warehouseName);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;

        // 绘制球体表示传送点位置
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        // 绘制箭头表示朝向
        Vector3 forward = transform.forward * gizmoRadius * 2f;
        Gizmos.DrawRay(transform.position, forward);

        // 绘制箭头头部
        Vector3 right = Quaternion.LookRotation(forward) * Quaternion.Euler(0, 160, 0) * Vector3.forward * 0.2f;
        Vector3 left = Quaternion.LookRotation(forward) * Quaternion.Euler(0, -160, 0) * Vector3.forward * 0.2f;
        Gizmos.DrawRay(transform.position + forward, right);
        Gizmos.DrawRay(transform.position + forward, left);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;

        // 选中时显示标签
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius + 0.1f);
    }
}

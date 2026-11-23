using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 传送点管理器 - 管理场景中所有仓库的传送点
/// 
/// 使用方法：
/// 1. 在场景中创建空物体，命名为 TeleportPointManager
/// 2. 添加本脚本
/// 3. 在Inspector中点击"创建所有传送点"按钮（编辑器模式下）
/// 4. 调整每个传送点的位置和朝向
/// 
/// 或者手动创建：
/// - 创建空物体 → 添加 WarehouseTeleportPoint 脚本 → 设置 warehouseName
/// </summary>
public class TeleportPointManager : MonoBehaviour
{
    [Header("传送点父物体")]
    public Transform teleportPointsParent;

    [Header("传送点列表")]
    public List<WarehouseTeleportPoint> teleportPoints = new List<WarehouseTeleportPoint>();

    [Header("默认偏移（创建时每个点之间的间距）")]
    public float defaultSpacing = 5f;

    /// <summary>
    /// 需要的仓库传送点名称
    /// </summary>
    public static readonly string[] RequiredWarehouses = new string[]
    {
        "main_menu",
        "apple",
        "frozen_meat",
        "chocolate"
    };

    void Awake()
    {
        if (teleportPointsParent == null)
        {
            teleportPointsParent = transform;
        }
    }

    void Start()
    {
        // 确保GameManager使用点位传送模式
        if (WarehouseGameManager.Instance != null)
        {
            if (WarehouseGameManager.Instance.loadMode != WarehouseLoadMode.Teleport)
            {
                Debug.Log("[TeleportPointManager] 自动设置为点位传送模式");
                WarehouseGameManager.Instance.loadMode = WarehouseLoadMode.Teleport;
            }
        }

        // 收集场景中的传送点
        CollectTeleportPoints();

        // 检查是否缺少传送点
        CheckMissingTeleportPoints();
    }

    /// <summary>
    /// 收集场景中所有传送点
    /// </summary>
    public void CollectTeleportPoints()
    {
        teleportPoints.Clear();

        WarehouseTeleportPoint[] points = FindObjectsOfType<WarehouseTeleportPoint>();
        teleportPoints.AddRange(points);

        Debug.Log($"[TeleportPointManager] 找到 {teleportPoints.Count} 个传送点");
    }

    /// <summary>
    /// 检查缺少的传送点并输出警告
    /// </summary>
    void CheckMissingTeleportPoints()
    {
        HashSet<string> existing = new HashSet<string>();
        foreach (var point in teleportPoints)
        {
            existing.Add(point.warehouseName);
        }

        foreach (string warehouse in RequiredWarehouses)
        {
            if (!existing.Contains(warehouse))
            {
                Debug.LogWarning($"[TeleportPointManager] 缺少传送点: {warehouse} " +
                                 $"({WarehouseGameManager.GetWarehouseDisplayName(warehouse)})");
            }
        }
    }

    /// <summary>
    /// 获取指定仓库的传送点
    /// </summary>
    public WarehouseTeleportPoint GetTeleportPoint(string warehouseName)
    {
        foreach (var point in teleportPoints)
        {
            if (point.warehouseName == warehouseName)
            {
                return point;
            }
        }
        return null;
    }

    /// <summary>
    /// 传送到指定仓库
    /// </summary>
    public void TeleportTo(string warehouseName)
    {
        WarehouseTeleportPoint point = GetTeleportPoint(warehouseName);
        if (point == null)
        {
            Debug.LogError($"[TeleportPointManager] 未找到传送点: {warehouseName}");
            return;
        }

        VRPlayerController player = FindObjectOfType<VRPlayerController>();
        if (player != null)
        {
            player.ResetPlayer(point.transform.position, point.transform.eulerAngles.y);
            Debug.Log($"[TeleportPointManager] 已传送到 {warehouseName}");
        }
        else
        {
            Debug.LogError("[TeleportPointManager] 未找到玩家控制器!");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器方法：创建所有需要的传送点
    /// </summary>
    [ContextMenu("创建所有传送点")]
    public void CreateAllTeleportPoints()
    {
        if (teleportPointsParent == null)
        {
            GameObject parentObj = new GameObject("TeleportPoints");
            parentObj.transform.SetParent(transform);
            teleportPointsParent = parentObj.transform;
        }

        // 收集已有传送点
        Dictionary<string, WarehouseTeleportPoint> existing = new Dictionary<string, WarehouseTeleportPoint>();
        WarehouseTeleportPoint[] allPoints = teleportPointsParent.GetComponentsInChildren<WarehouseTeleportPoint>();
        foreach (var p in allPoints)
        {
            existing[p.warehouseName] = p;
        }

        // 创建缺少的传送点
        for (int i = 0; i < RequiredWarehouses.Length; i++)
        {
            string warehouse = RequiredWarehouses[i];

            if (existing.ContainsKey(warehouse))
            {
                Debug.Log($"[TeleportPointManager] 传送点已存在: {warehouse}");
                continue;
            }

            string displayName = WarehouseGameManager.GetWarehouseDisplayName(warehouse);
            GameObject pointObj = new GameObject($"TeleportPoint_{warehouse}");
            pointObj.transform.SetParent(teleportPointsParent, false);

            // 设置默认位置（按顺序排列）
            pointObj.transform.localPosition = new Vector3(i * defaultSpacing, 0f, 0f);

            WarehouseTeleportPoint tp = pointObj.AddComponent<WarehouseTeleportPoint>();
            tp.warehouseName = warehouse;

            Debug.Log($"[TeleportPointManager] 创建传送点: {warehouse} ({displayName})");
        }

        // 刷新列表
        CollectTeleportPoints();
    }

    /// <summary>
    /// 编辑器方法：选中所有传送点
    /// </summary>
    [ContextMenu("选中所有传送点")]
    public void SelectAllTeleportPoints()
    {
        CollectTeleportPoints();

        List<GameObject> objs = new List<GameObject>();
        foreach (var p in teleportPoints)
        {
            objs.Add(p.gameObject);
        }

        UnityEditor.Selection.objects = objs.ToArray();
    }

    /// <summary>
    /// 编辑器方法：重置所有传送点位置
    /// </summary>
    [ContextMenu("重置传送点位置")]
    public void ResetTeleportPointPositions()
    {
        CollectTeleportPoints();

        Dictionary<string, int> orderMap = new Dictionary<string, int>();
        for (int i = 0; i < RequiredWarehouses.Length; i++)
        {
            orderMap[RequiredWarehouses[i]] = i;
        }

        foreach (var p in teleportPoints)
        {
            if (orderMap.TryGetValue(p.warehouseName, out int index))
            {
                p.transform.localPosition = new Vector3(index * defaultSpacing, 0f, 0f);
                p.transform.localRotation = Quaternion.identity;
                UnityEditor.EditorUtility.SetDirty(p);
            }
        }
    }
#endif
}

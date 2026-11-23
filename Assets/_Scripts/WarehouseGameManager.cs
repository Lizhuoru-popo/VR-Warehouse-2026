using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 仓库跳转模式
/// </summary>
public enum WarehouseLoadMode
{
    /// <summary>
    /// 场景切换模式 - 每个仓库是独立的场景
    /// </summary>
    SceneSwitch,

    /// <summary>
    /// 点位传送模式 - 所有仓库在同一场景中，传送到不同点位
    /// </summary>
    Teleport
}

/// <summary>
/// 仓库VR游戏管理器 - 管理场景切换/传送、全局状态
/// 
/// 支持两种跳转模式：
/// 1. 场景切换模式 - 每个仓库是独立的场景（需要在Build Settings中添加场景）
/// 2. 点位传送模式 - 所有仓库在同一场景中（需要在场景中放置传送点）
/// </summary>
public class WarehouseGameManager : MonoBehaviour
{
    public static WarehouseGameManager Instance { get; private set; }

    [Header("跳转模式")]
    [Tooltip("SceneSwitch=场景切换，Teleport=同场景点位传送")]
    public WarehouseLoadMode loadMode = WarehouseLoadMode.Teleport;

    [Header("场景名称（场景切换模式使用）")]
    public string mainMenuSceneName = "SampleScene";
    public string warehouseSceneName = "VR Warehouse";

    [Header("当前状态")]
    public string currentWarehouse = "";

    // 传送点字典 - 运行时由WarehouseTeleportPoint注册
    private System.Collections.Generic.Dictionary<string, Transform> teleportPoints =
        new System.Collections.Generic.Dictionary<string, Transform>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 注册传送点（由WarehouseTeleportPoint调用）
    /// </summary>
    public void RegisterTeleportPoint(string warehouseName, Transform point)
    {
        if (teleportPoints.ContainsKey(warehouseName))
        {
            teleportPoints[warehouseName] = point;
        }
        else
        {
            teleportPoints.Add(warehouseName, point);
        }

        Debug.Log($"[GameManager] 注册传送点: {warehouseName} -> {point.position}");
    }

    /// <summary>
    /// 注销传送点
    /// </summary>
    public void UnregisterTeleportPoint(string warehouseName)
    {
        if (teleportPoints.ContainsKey(warehouseName))
        {
            teleportPoints.Remove(warehouseName);
        }
    }

    /// <summary>
    /// 获取指定仓库的传送点
    /// </summary>
    public Transform GetTeleportPoint(string warehouseName)
    {
        if (teleportPoints.TryGetValue(warehouseName, out Transform point))
        {
            return point;
        }
        return null;
    }

    /// <summary>
    /// 跳转到指定仓库
    /// 根据 loadMode 自动选择场景切换或点位传送
    /// </summary>
    public void LoadWarehouse(string warehouseName)
    {
        currentWarehouse = warehouseName;
        Debug.Log($"[GameManager] 进入仓库: {warehouseName} (模式: {loadMode})");

        if (loadMode == WarehouseLoadMode.SceneSwitch)
        {
            // 场景切换模式
            if (!string.IsNullOrEmpty(warehouseSceneName))
            {
                SceneManager.LoadScene(warehouseSceneName);
            }
            else
            {
                Debug.LogError("[GameManager] 未设置仓库场景名称!");
            }
        }
        else
        {
            // 点位传送模式 - 通知场景中的玩家传送到指定点
            TeleportPlayerToWarehouse(warehouseName);
        }
    }

    /// <summary>
    /// 点位传送 - 将玩家传送到指定仓库的传送点
    /// </summary>
    void TeleportPlayerToWarehouse(string warehouseName)
    {
        Transform targetPoint = GetTeleportPoint(warehouseName);

        if (targetPoint == null)
        {
            Debug.LogError($"[GameManager] 未找到仓库 {warehouseName} 的传送点! " +
                           $"请在场景中添加 WarehouseTeleportPoint 组件。");
            return;
        }

        // 使用 VRPlayerController 传送
        VRPlayerController player = FindObjectOfType<VRPlayerController>();
        if (player != null)
        {
            player.ResetPlayer(targetPoint.position, targetPoint.eulerAngles.y);
            Debug.Log($"[GameManager] 玩家已传送到 {warehouseName}: {targetPoint.position}");
            return;
        }

        // 备用：通过 Camera.main 查找玩家
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Transform playerRoot = mainCam.transform.root;
            CharacterController cc = playerRoot.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerRoot.position = targetPoint.position;
            playerRoot.rotation = Quaternion.Euler(0f, targetPoint.eulerAngles.y, 0f);
            if (cc != null) cc.enabled = true;
            Debug.Log($"[GameManager] 玩家已通过Camera传送到 {warehouseName}");
            return;
        }

        Debug.LogError("[GameManager] 未找到玩家，无法传送!");
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        currentWarehouse = "";
        Debug.Log("[GameManager] 返回主菜单");

        if (loadMode == WarehouseLoadMode.SceneSwitch)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // 点位传送模式下，传送到主菜单点
            TeleportPlayerToWarehouse("main_menu");
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] 退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 检查当前是否在仓库中
    /// </summary>
    public bool IsInWarehouse()
    {
        return !string.IsNullOrEmpty(currentWarehouse);
    }

    /// <summary>
    /// 获取仓库显示名称（中文）
    /// </summary>
    public static string GetWarehouseDisplayName(string warehouseName)
    {
        switch (warehouseName)
        {
            case "frozen_meat":
                return "冷冻肉类仓";
            case "apple":
                return "苹果保鲜仓";
            case "chocolate":
                return "巧克力恒温仓";
            default:
                return warehouseName;
        }
    }
}

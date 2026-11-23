using UnityEngine;

/// <summary>
/// 场景初始化器 - 自动初始化所有系统
/// 
/// 功能：
/// 1. 自动创建 WarehouseGameManager（如果不存在）
/// 2. 自动设置为点位传送模式
/// 3. 自动绑定玩家控制器和仓库选择面板
/// 4. 游戏开始时将玩家传送到 main_menu 传送点
/// </summary>
public class SceneInitializer : MonoBehaviour
{
    [Header("游戏管理器设置")]
    public bool autoCreateGameManager = true;
    public WarehouseLoadMode defaultLoadMode = WarehouseLoadMode.Teleport;

    [Header("玩家设置")]
    public VRPlayerController playerController;

    [Header("仓库选择UI")]
    public WarehouseSelectController warehouseSelectUI;

    [Header("起始传送点")]
    [Tooltip("游戏开始时传送到哪个点，默认 main_menu")]
    public string startWarehouse = "main_menu";

    void Awake()
    {
        if (autoCreateGameManager && WarehouseGameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("WarehouseGameManager");
            WarehouseGameManager gm = gmObj.AddComponent<WarehouseGameManager>();
            gm.loadMode = defaultLoadMode;
            Debug.Log($"[SceneInitializer] 创建 WarehouseGameManager (模式: {defaultLoadMode})");
        }
        else if (WarehouseGameManager.Instance != null)
        {
            if (WarehouseGameManager.Instance.loadMode != defaultLoadMode)
            {
                WarehouseGameManager.Instance.loadMode = defaultLoadMode;
                Debug.Log($"[SceneInitializer] 设置为 {defaultLoadMode} 模式");
            }
        }
    }

    void Start()
    {
        if (playerController == null)
            playerController = FindObjectOfType<VRPlayerController>();

        // 自修复：场景中缺少玩家控制器时，自动挂载到 XR Origin 上
        // （XR Origin 上的移动/旋转由 XRI 组件处理，VRPlayerController 只负责传送/锁定/菜单键）
        if (playerController == null)
        {
            Unity.XR.CoreUtils.XROrigin xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                playerController = xrOrigin.gameObject.AddComponent<VRPlayerController>();
                Debug.Log("[SceneInitializer] 已自动为 XR Origin 添加 VRPlayerController 组件");
            }
        }

        // 绑定仓库选择面板到玩家控制器
        if (playerController != null && warehouseSelectUI != null)
        {
            if (playerController.warehouseSelectUI == null)
            {
                playerController.warehouseSelectUI = warehouseSelectUI;
                Debug.Log("[SceneInitializer] 仓库选择面板已绑定到玩家控制器");
            }

            if (warehouseSelectUI.playerController == null)
            {
                warehouseSelectUI.playerController = playerController;
                Debug.Log("[SceneInitializer] 玩家控制器已绑定到仓库选择面板");
            }
        }

        StartCoroutine(TeleportToStartPointDelayed());
    }

    System.Collections.IEnumerator TeleportToStartPointDelayed()
    {
        yield return null;
        yield return null;

        if (WarehouseGameManager.Instance != null &&
            WarehouseGameManager.Instance.loadMode == WarehouseLoadMode.Teleport)
        {
            Transform startPoint = WarehouseGameManager.Instance.GetTeleportPoint(startWarehouse);

            if (startPoint != null && playerController != null)
            {
                playerController.ResetPlayer(startPoint.position, startPoint.eulerAngles.y);
                Debug.Log($"[SceneInitializer] 玩家已传送到起始点: {startWarehouse}");
            }
            else
            {
                Debug.LogWarning($"[SceneInitializer] 无法传送到起始点 {startWarehouse}: " +
                                 $"传送点存在={startPoint != null}, 玩家存在={playerController != null}。" +
                                 "请确认场景中已放置 XR Origin (XR Rig) 预制体。");
            }
        }
    }
}

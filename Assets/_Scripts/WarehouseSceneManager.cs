using UnityEngine;

/// <summary>
/// 仓库场景管理器 - 管理场景中的玩家和仓库选择UI
/// </summary>
public class WarehouseSceneManager : MonoBehaviour
{
    [Header("玩家引用")]
    public VRPlayerController playerController;

    [Header("仓库选择UI")]
    public WarehouseSelectController warehouseSelectUI;

    [Header("传感器UI")]
    public SensorUIManager sensorUIManager;

    void Awake()
    {
        if (WarehouseGameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("WarehouseGameManager");
            gmObj.AddComponent<WarehouseGameManager>();
        }
    }

    void Start()
    {
        InitializePlayer();
        Debug.Log("[WarehouseScene] 场景初始化完成");
    }

    void InitializePlayer()
    {
        if (playerController == null)
            playerController = FindObjectOfType<VRPlayerController>();

        if (playerController == null)
        {
            Debug.LogWarning("[WarehouseScene] 未找到 VRPlayerController!");
            return;
        }

        // 绑定仓库选择面板到玩家控制器
        if (warehouseSelectUI != null && playerController.warehouseSelectUI == null)
        {
            playerController.warehouseSelectUI = warehouseSelectUI;
            Debug.Log("[WarehouseScene] 仓库选择面板已绑定到玩家控制器");
        }

        // 绑定玩家控制器到仓库选择面板
        if (warehouseSelectUI != null && warehouseSelectUI.playerController == null)
        {
            warehouseSelectUI.playerController = playerController;
            Debug.Log("[WarehouseScene] 玩家控制器已绑定到仓库选择面板");
        }
    }

    /// <summary>
    /// 切换到指定仓库
    /// </summary>
    public void SwitchWarehouse(string targetWarehouse)
    {
        if (WarehouseGameManager.Instance != null)
        {
            WarehouseGameManager.Instance.LoadWarehouse(targetWarehouse);
        }
    }
}

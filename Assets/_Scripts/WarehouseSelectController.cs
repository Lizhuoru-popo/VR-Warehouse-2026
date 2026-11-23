using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 仓库选择面板控制器
/// 
/// UI结构：
/// - 世界画布上只有一个"仓库选择"按钮
/// - 点击后打开仓库选择面板
/// - 面板上有5个按钮：
///   1. 苹果保鲜仓 → 跳转到apple仓，关闭当前UI
///   2. 冷冻肉类仓 → 跳转到frozen_meat仓，关闭当前UI
///   3. 巧克力恒温仓 → 跳转到chocolate仓，关闭当前UI
///   4. 关闭界面 → 关闭当前UI
///   5. 退出仓库 → 结束游戏
/// </summary>
public class WarehouseSelectController : MonoBehaviour
{
    [Header("仓库选择按钮（世界画布上的那个按钮）")]
    public Button openSelectButton;
    public TextMeshProUGUI openSelectButtonText;

    [Header("仓库选择面板")]
    public GameObject selectPanel;

    [Header("苹果保鲜仓按钮")]
    public Button appleButton;
    public TextMeshProUGUI appleButtonText;

    [Header("冷冻肉类仓按钮")]
    public Button frozenMeatButton;
    public TextMeshProUGUI frozenMeatButtonText;

    [Header("巧克力恒温仓按钮")]
    public Button chocolateButton;
    public TextMeshProUGUI chocolateButtonText;

    [Header("关闭界面按钮")]
    public Button closeButton;
    public TextMeshProUGUI closeButtonText;

    [Header("退出仓库按钮")]
    public Button quitButton;
    public TextMeshProUGUI quitButtonText;

    [Header("玩家控制器")]
    public VRPlayerController playerController;

    void Start()
    {
        // 初始化按钮文本
        if (openSelectButtonText != null)
            openSelectButtonText.text = "仓库选择";

        if (appleButtonText != null)
            appleButtonText.text = "苹果保鲜仓";

        if (frozenMeatButtonText != null)
            frozenMeatButtonText.text = "冷冻肉类仓";

        if (chocolateButtonText != null)
            chocolateButtonText.text = "巧克力恒温仓";

        if (closeButtonText != null)
            closeButtonText.text = "关闭界面";

        if (quitButtonText != null)
            quitButtonText.text = "退出仓库";

        // 注册按钮事件
        if (openSelectButton != null)
            openSelectButton.onClick.AddListener(OnOpenSelectClicked);

        if (appleButton != null)
            appleButton.onClick.AddListener(OnAppleClicked);

        if (frozenMeatButton != null)
            frozenMeatButton.onClick.AddListener(OnFrozenMeatClicked);

        if (chocolateButton != null)
            chocolateButton.onClick.AddListener(OnChocolateClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        // 初始状态：显示打开按钮，隐藏选择面板
        if (openSelectButton != null && openSelectButton.gameObject != null)
            openSelectButton.gameObject.SetActive(true);

        if (selectPanel != null)
            selectPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (openSelectButton != null)
            openSelectButton.onClick.RemoveListener(OnOpenSelectClicked);

        if (appleButton != null)
            appleButton.onClick.RemoveListener(OnAppleClicked);

        if (frozenMeatButton != null)
            frozenMeatButton.onClick.RemoveListener(OnFrozenMeatClicked);

        if (chocolateButton != null)
            chocolateButton.onClick.RemoveListener(OnChocolateClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(OnCloseClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    /// <summary>
    /// 点击仓库选择按钮 - 打开面板
    /// </summary>
    void OnOpenSelectClicked()
    {
        Debug.Log("[WarehouseSelect] 点击仓库选择按钮，打开面板");
        OpenPanel();
    }

    /// <summary>
    /// 打开仓库选择面板 - 锁定视角，面板定位到玩家面前中心
    /// 仓库选择按钮保持显示，不会消失
    /// </summary>
    public void OpenPanel()
    {
        if (selectPanel != null)
        {
            selectPanel.SetActive(true);
            PositionPanelInFrontOfPlayer();
        }

        // 锁定玩家输入
        if (playerController != null)
            playerController.LockInput();

        Debug.Log("[WarehouseSelect] 面板已打开，视角已锁定");
    }

    /// <summary>
    /// 关闭仓库选择面板 - 解锁视角
    /// </summary>
    public void ClosePanel()
    {
        if (selectPanel != null)
            selectPanel.SetActive(false);

        // 解锁玩家输入
        if (playerController != null)
            playerController.UnlockInput();

        Debug.Log("[WarehouseSelect] 面板已关闭，视角已解锁");
    }

    /// <summary>
    /// 将面板定位到玩家正前方中心
    /// 优先使用 VRUIFollow 组件，如果没有则手动计算位置
    /// </summary>
    void PositionPanelInFrontOfPlayer()
    {
        if (selectPanel == null) return;

        // 优先使用 VRUIFollow
        VRUIFollow follow = selectPanel.GetComponent<VRUIFollow>();
        if (follow != null)
        {
            follow.SnapToTarget();
            return;
        }

        // 备用：手动计算位置
        Camera cam = Camera.main;
        if (cam == null) return;

        // 只取yaw方向（水平方向）
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        // 放在玩家正前方2米，与视线同高
        float panelDistance = 2f;
        Vector3 panelPos = cam.transform.position + forward * panelDistance;
        panelPos.y = cam.transform.position.y;

        selectPanel.transform.position = panelPos;

        // 面向玩家（UI 正面朝向玩家，让 -Z 指向玩家）
        Vector3 panelForward = panelPos - cam.transform.position;
        panelForward.y = 0f;
        if (panelForward.sqrMagnitude > 0.01f)
        {
            selectPanel.transform.rotation = Quaternion.LookRotation(panelForward);
        }
    }

    /// <summary>
    /// 切换仓库选择面板显示/隐藏（B键调用）
    /// </summary>
    public void TogglePanel()
    {
        if (selectPanel == null)
        {
            Debug.LogWarning("[WarehouseSelect] 选择面板未设置!");
            return;
        }

        bool isActive = selectPanel.activeSelf;
        selectPanel.SetActive(!isActive);
        Debug.Log($"[WarehouseSelect] 面板: {(isActive ? "关闭" : "打开")}");
    }

    /// <summary>
    /// 切换仓库选择按钮显示/隐藏（左手柄抓握键调用）
    /// </summary>
    public void ToggleOpenButton()
    {
        if (openSelectButton == null || openSelectButton.gameObject == null)
        {
            Debug.LogWarning("[WarehouseSelect] 仓库选择按钮未设置!");
            return;
        }

        bool isActive = openSelectButton.gameObject.activeSelf;
        openSelectButton.gameObject.SetActive(!isActive);
        Debug.Log($"[WarehouseSelect] 仓库选择按钮: {(!isActive ? "显示" : "隐藏")}");
    }

    /// <summary>
    /// 苹果保鲜仓按钮点击
    /// </summary>
    void OnAppleClicked()
    {
        Debug.Log("[WarehouseSelect] 选择 苹果保鲜仓 (apple)");
        ClosePanel();
        LoadWarehouse("apple");
    }

    /// <summary>
    /// 冷冻肉类仓按钮点击
    /// </summary>
    void OnFrozenMeatClicked()
    {
        Debug.Log("[WarehouseSelect] 选择 冷冻肉类仓 (frozen_meat)");
        ClosePanel();
        LoadWarehouse("frozen_meat");
    }

    /// <summary>
    /// 巧克力恒温仓按钮点击
    /// </summary>
    void OnChocolateClicked()
    {
        Debug.Log("[WarehouseSelect] 选择 巧克力恒温仓 (chocolate)");
        ClosePanel();
        LoadWarehouse("chocolate");
    }

    /// <summary>
    /// 关闭界面按钮点击
    /// </summary>
    void OnCloseClicked()
    {
        Debug.Log("[WarehouseSelect] 关闭界面");
        ClosePanel();
    }

    /// <summary>
    /// 退出仓库按钮点击 - 结束游戏
    /// </summary>
    void OnQuitClicked()
    {
        Debug.Log("[WarehouseSelect] 退出游戏");
        ClosePanel();

        if (WarehouseGameManager.Instance != null)
        {
            WarehouseGameManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>
    /// 加载/传送到指定仓库
    /// </summary>
    void LoadWarehouse(string warehouseName)
    {
        if (WarehouseGameManager.Instance != null)
        {
            WarehouseGameManager.Instance.LoadWarehouse(warehouseName);
        }
        else
        {
            Debug.LogError("[WarehouseSelect] WarehouseGameManager 实例不存在!");
        }
    }
}

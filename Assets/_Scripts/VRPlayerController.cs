using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// VR玩家控制器（XRI 3.x 适配版）- 挂载在 XR Origin (XR Rig) 根物体上
///
/// 移动/旋转/重力/点击交互已由 XRI 组件接管（输入来自 XRI Default Input Actions）：
///   - Dynamic Move Provider  : 左摇杆移动 + 重力
///   - Snap/Continuous Turn   : 右摇杆旋转
///   - Teleportation Provider : 传送
///   - Ray/Direct Interactor  : UI点击与抓取
///
/// 本脚本只保留：
///   1. ResetPlayer      - 传送玩家（移动 XR Origin 根物体）
///   2. Lock/UnlockInput - 锁定/恢复 XRI 移动组件（打开面板时由 WarehouseSelectController 调用）
///   3. 菜单键（默认右手柄B键）开关仓库选择面板
///   4. 左手柄抓握键 - 开关仓库选择按钮显示/隐藏
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VRPlayerController : MonoBehaviour
{
    [Header("仓库选择面板")]
    public WarehouseSelectController warehouseSelectUI;

    [Header("菜单键")]
    [Tooltip("默认绑定右手柄B键（含XR设备模拟器），可在Inspector中改为其他输入")]
    public InputActionProperty menuAction;

    [Header("左手柄抓握键")]
    [Tooltip("默认绑定左手柄抓握键（含XR设备模拟器），切换仓库选择按钮显示/隐藏")]
    public InputActionProperty gripAction;

    [Header("调试模式")]
    public bool enableDebugLog = false;

    private CharacterController characterController;
    private readonly List<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider> locomotionProviders = new List<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>();
    private readonly Dictionary<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider, bool> lockedProviderStates =
        new Dictionary<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider, bool>();

    void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // 未在Inspector中指定菜单键时，使用默认绑定（右手柄B键 + XR设备模拟器）
        if (menuAction.action == null || menuAction.action.bindings.Count == 0)
        {
            var menu = new InputAction("Menu", InputActionType.Button);
            menu.AddBinding("<XRController>{RightHand}/secondaryButton");
            menu.AddBinding("<XRSimulatedController>/secondaryButton");
            menuAction = new InputActionProperty(menu);
        }

        // 未在Inspector中指定左手柄抓握键时，使用默认绑定（左手柄grip键 + XR设备模拟器左手 + PC键盘兜底）
        if (gripAction.action == null || gripAction.action.bindings.Count == 0)
        {
            var grip = new InputAction("Grip", InputActionType.Button);
            grip.AddBinding("<XRController>{LeftHand}/gripButton");
            grip.AddBinding("<XRSimulatedController>{LeftHand}/gripButton");
            grip.AddBinding("<Keyboard>/g");
            gripAction = new InputActionProperty(grip);
        }
    }

    void OnEnable()
    {
        var action = menuAction.action;
        if (action != null)
        {
            action.performed += OnMenuPerformed;
            action.Enable();
        }

        var grip = gripAction.action;
        if (grip != null)
            grip.Enable();
    }

    void OnDisable()
    {
        var action = menuAction.action;
        if (action != null)
        {
            action.performed -= OnMenuPerformed;
            action.Disable();
        }

        var grip = gripAction.action;
        if (grip != null)
            grip.Disable();
    }

    private void OnMenuPerformed(InputAction.CallbackContext context)
    {
        if (!context.ReadValueAsButton()) return;

        if (enableDebugLog)
            Debug.Log("[VRPlayer] 菜单键按下 - 切换仓库选择面板");

        if (warehouseSelectUI != null)
            warehouseSelectUI.TogglePanel();
        else
            Debug.LogWarning("[VRPlayer] 仓库选择面板未绑定!");
    }

    // 上一帧左手柄抓握键状态，用于上升沿检测（按一下只触发一次）
    private bool m_GripWasPressed = false;

    void Update()
    {
        // 检测左手柄抓握键上升沿：本帧按下且上一帧未按下 → 触发一次切换
        bool gripPressed = IsGripPressed();
        if (gripPressed && !m_GripWasPressed)
        {
            if (enableDebugLog)
                Debug.Log("[VRPlayer] 左手柄抓握键按下 - 切换仓库选择按钮显示");

            if (warehouseSelectUI != null)
                warehouseSelectUI.ToggleOpenButton();
            else
                Debug.LogWarning("[VRPlayer] 仓库选择面板未绑定!");
        }
        m_GripWasPressed = gripPressed;
    }

    /// <summary>
    /// 检测左手柄抓握键是否被按下
    /// 优先用 gripAction（真机PICO和模拟器路径），失败时用键盘G兜底（PC模拟）
    /// </summary>
    private bool IsGripPressed()
    {
        var grip = gripAction.action;
        if (grip != null && grip.enabled && grip.IsPressed())
            return true;

        // PC 模拟兜底：键盘 G 键
        if (Keyboard.current != null && Keyboard.current.gKey.isPressed)
            return true;

        return false;
    }

    /// <summary>
    /// 重置玩家位置和旋转（传送用）：移动 XR Origin 根物体，头部高度由 Camera Offset 保持
    /// </summary>
    public void ResetPlayer(Vector3 position, float yRotation)
    {
        if (characterController != null)
            characterController.enabled = false;

        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        if (characterController != null)
            characterController.enabled = true;

        // 传送后关闭仓库选择面板
        if (warehouseSelectUI != null)
            warehouseSelectUI.ClosePanel();

        if (enableDebugLog)
            Debug.Log($"[VRPlayer] 传送到: {position}, 朝向: {yRotation}");
    }

    /// <summary>
    /// 锁定移动输入：临时禁用 XR Origin 下的所有 LocomotionProvider
    /// （移动/旋转/攀爬/抓取移动/传送），恢复时还原各自原本的启用状态
    /// </summary>
    public void LockInput()
    {
        lockedProviderStates.Clear();
        locomotionProviders.Clear();
        GetComponentsInChildren(true, locomotionProviders);

        foreach (var provider in locomotionProviders)
        {
            if (provider == null) continue;
            lockedProviderStates[provider] = provider.enabled;
            provider.enabled = false;
        }

        if (enableDebugLog)
            Debug.Log($"[VRPlayer] 输入已锁定 ({lockedProviderStates.Count} 个移动组件)");
    }

    /// <summary>
    /// 解锁移动输入：恢复锁定前的移动组件状态
    /// </summary>
    public void UnlockInput()
    {
        foreach (var pair in lockedProviderStates)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }
        lockedProviderStates.Clear();

        if (enableDebugLog)
            Debug.Log("[VRPlayer] 输入已解锁");
    }

    /// <summary>
    /// 传送到指定仓库
    /// </summary>
    public void TeleportToWarehouse(string warehouseName)
    {
        if (WarehouseGameManager.Instance == null)
        {
            Debug.LogError("[VRPlayer] WarehouseGameManager 不存在!");
            return;
        }

        Transform targetPoint = WarehouseGameManager.Instance.GetTeleportPoint(warehouseName);
        if (targetPoint == null)
        {
            Debug.LogError($"[VRPlayer] 未找到传送点: {warehouseName}");
            return;
        }

        ResetPlayer(targetPoint.position, targetPoint.eulerAngles.y);
    }
}

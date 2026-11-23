using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public class DisplaySensorButton : MonoBehaviour
{
    [Header("目标传感器")]
    public SensorController targetSensor;

    [Header("目标UI面板（可选）")]
    [Tooltip("如果留空，使用 SensorUIManager.Instance 单例面板；如果设置了，使用这个面板显示")]
    public SensorUIManager targetPanel;

    [Header("悬停效果")]
    public bool enableScaleEffect = true;
    public float hoverScale = 1.1f;
    public bool enableHighlight = true;
    public Material highlightMaterial;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Vector3 originalScale;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        originalScale = transform.localScale;
    }

    void Start()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

        if (interactable == null)
        {
            Debug.LogError("[DisplayButton] 找不到 XRBaseInteractable 组件，请添加 XR Simple Interactable", gameObject);
            return;
        }

        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("[DisplayButton] 没有 Collider（碰撞体），按钮无法被射线检测到！请添加 Box Collider 等碰撞体", gameObject);
        }

        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);

        Debug.Log("[DisplayButton] 按钮初始化完成: " + gameObject.name, gameObject);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("[DisplayButton] 按钮被点击: " + gameObject.name, gameObject);

        if (targetSensor == null)
        {
            Debug.LogError("[DisplayButton] 未设置 Target Sensor！请在 Inspector 中拖入目标传感器物体", gameObject);
            return;
        }

        SensorUIManager panel = targetPanel != null ? targetPanel : SensorUIManager.Instance;

        if (panel == null)
        {
            Debug.LogError("[DisplayButton] 找不到可用的 SensorUIManager 面板！请设置 targetPanel 或确保场景中有 Instance 面板");
            return;
        }

        panel.ShowSensor(targetSensor);
        Debug.Log("[DisplayButton] 已打开传感器UI: " + targetSensor.sensorId);
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log("[DisplayButton] 悬停进入: " + gameObject.name, gameObject);

        if (enableScaleEffect)
        {
            transform.localScale = originalScale * hoverScale;
        }

        if (enableHighlight && highlightMaterial != null && meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
            meshRenderer.material = highlightMaterial;
        }
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log("[DisplayButton] 悬停退出: " + gameObject.name, gameObject);

        if (enableScaleEffect)
        {
            transform.localScale = originalScale;
        }

        if (enableHighlight && originalMaterial != null && meshRenderer != null)
        {
            meshRenderer.material = originalMaterial;
        }
    }
}

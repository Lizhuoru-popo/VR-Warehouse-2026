using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR通用按钮脚本 - 支持XR交互和点击事件
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
public class VRButton : MonoBehaviour
{
    [Header("按钮文本")]
    public TextMeshProUGUI buttonText;
    public string textContent = "按钮";

    [Header("点击事件")]
    public UnityEvent onClick;

    [Header("悬停效果")]
    public bool enableScaleEffect = true;
    public float hoverScale = 1.1f;
    public bool enableColorChange = true;
    public Color hoverColor = new Color(0.8f, 0.9f, 1f);

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private MeshRenderer meshRenderer;
    private Image buttonImage;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isInitialized = false;

    void Awake()
    {
        originalScale = transform.localScale;
        meshRenderer = GetComponent<MeshRenderer>();
        buttonImage = GetComponent<Image>();

        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }
        else if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }

        if (buttonText != null && !string.IsNullOrEmpty(textContent))
        {
            buttonText.text = textContent;
        }
    }

    void Start()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();

        if (interactable == null)
        {
            Debug.LogError("[VRButton] 找不到 XRBaseInteractable 组件!", gameObject);
            return;
        }

        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);

        isInitialized = true;
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    /// <summary>
    /// 设置按钮文本
    /// </summary>
    public void SetText(string text)
    {
        textContent = text;
        if (buttonText != null)
        {
            buttonText.text = text;
        }
    }

    /// <summary>
    /// 模拟点击（供代码调用）
    /// </summary>
    public void SimulateClick()
    {
        onClick?.Invoke();
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("[VRButton] 按钮被点击: " + gameObject.name);
        onClick?.Invoke();
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (enableScaleEffect)
        {
            transform.localScale = originalScale * hoverScale;
        }

        if (enableColorChange)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = hoverColor;
            }
            else if (buttonImage != null)
            {
                buttonImage.color = hoverColor;
            }
        }
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        if (enableScaleEffect)
        {
            transform.localScale = originalScale;
        }

        if (enableColorChange)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = originalColor;
            }
            else if (buttonImage != null)
            {
                buttonImage.color = originalColor;
            }
        }
    }
}

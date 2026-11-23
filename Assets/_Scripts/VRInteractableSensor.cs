using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable))]
[RequireComponent(typeof(MeshRenderer))]
public class VRInteractableSensor : MonoBehaviour
{
    [Header("悬停效果")]
    public bool enableScaleEffect = true;
    public float hoverScale = 1.2f;
    public bool enableHighlight = true;
    public Material highlightMaterial;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;
    private SensorController sensor;
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
        sensor = GetComponent<SensorController>();

        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        sensor?.OnSelect();
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
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
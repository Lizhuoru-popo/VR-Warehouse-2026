using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class SensorHighlight : MonoBehaviour
{
    public Material highlightMat;
    private MeshRenderer meshRender;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Material matBeforeHover;

    void Awake()
    {
        meshRender = GetComponent<MeshRenderer>();
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        matBeforeHover = meshRender.material;  // ��¼��ǰ״̬����
        meshRender.material = highlightMat;
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        if (matBeforeHover != null)
            meshRender.material = matBeforeHover;
    }
}
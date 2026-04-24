using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(AtomController))]
public class AtomGrabHandler : MonoBehaviour
{
    private XRGrabInteractable grab;

    private void Awake() => grab = GetComponent<XRGrabInteractable>();
    private void OnEnable() => grab.selectExited.AddListener(OnRelease);
    private void OnDisable() => grab.selectExited.RemoveListener(OnRelease);

    private void OnRelease(SelectExitEventArgs args)
    {
        Debug.Log($"[AtomGrabHandler] '{name}' released.");
    }
}
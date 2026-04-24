using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Lightweight grab handler. Bonding happens via OnTriggerEnter in AtomController.
/// Keep this on your atom prefab alongside AtomController.
/// </summary>
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
        // Bonding happens on touch (OnTriggerEnter), not here.
        // Add drop sound or haptics here if needed later.
        Debug.Log($"[AtomGrabHandler] '{name}' released.");
    }
}
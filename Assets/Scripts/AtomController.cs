using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AtomController : MonoBehaviour
{
    [Header("Atom Settings")]
    public string atomType = "H";
    public int maxBonds = 1;

    [Header("Materials")]
    public Material defaultMaterial;
    public Material grabbedMaterial;
    public Material bondedMaterial;
    public Material saturatedMaterial;

    [HideInInspector] public XRGrabInteractable grabInteractable;
    [HideInInspector] public List<SnapPoint> snapPoints = new List<SnapPoint>();
    [HideInInspector] public bool isBeingHeld = false;
    [HideInInspector] public bool isLocked = false;
    [HideInInspector] public bool hasBeenGrabbed = false;

    private Renderer atomRenderer;
    private List<AtomController> bondedAtoms = new List<AtomController>();

    public int BondCount => bondedAtoms.Count;
    public bool CanBond() => bondedAtoms.Count < maxBonds;
    public bool IsSaturated() => bondedAtoms.Count >= maxBonds;
    public bool IsBondedWith(AtomController other) => bondedAtoms.Contains(other);

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        snapPoints.AddRange(GetComponentsInChildren<SnapPoint>());
        atomRenderer = GetComponentInChildren<Renderer>();
    }

    private void Start()
    {
        ApplyMaterial(defaultMaterial);
    }

    private void OnEnable()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.AddListener(_ => OnGrabbed());
        grabInteractable.selectExited.AddListener(_ => OnReleased());
    }

    private void OnDisable()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.RemoveAllListeners();
        grabInteractable.selectExited.RemoveAllListeners();
    }

    private void OnGrabbed()
    {
        isBeingHeld = true;
        hasBeenGrabbed = true;
        ApplyMaterial(grabbedMaterial);
    }

    private void OnReleased()
    {
        isBeingHeld = false;
        RefreshMaterial();
    }

    public void AddBond(AtomController other)
    {
        if (!bondedAtoms.Contains(other))
            bondedAtoms.Add(other);
    }

    public void RemoveBond(AtomController other) => bondedAtoms.Remove(other);

    public SnapPoint GetFreeSnapPoint()
    {
        foreach (var sp in snapPoints)
            if (!sp.isOccupied) return sp;
        return null;
    }

    public void CheckAndLockIfSaturated()
    {
        RefreshMaterial();
        if (!IsSaturated()) return;
        isLocked = true;
    }

    public void RefreshMaterial()
    {
        if (isLocked)
            ApplyMaterial(saturatedMaterial);
        else if (BondCount > 0)
            ApplyMaterial(bondedMaterial);
        else
            ApplyMaterial(defaultMaterial);
    }

    private void ApplyMaterial(Material mat)
    {
        if (atomRenderer == null || mat == null) return;
        atomRenderer.material = mat;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasBeenGrabbed) return;
        if (isLocked) return;
        if (!isBeingHeld) return;

        AtomController otherAtom = other.GetComponentInParent<AtomController>();
        if (otherAtom == null || otherAtom == this) return;
        if (!otherAtom.hasBeenGrabbed) return;
        if (otherAtom.isLocked) return;

        BondManager.Instance.TrySnapBond(this, otherAtom);
    }

    private void OnDestroy()
    {
        foreach (var p in new List<AtomController>(bondedAtoms))
            p.RemoveBond(this);
        bondedAtoms.Clear();
    }
}
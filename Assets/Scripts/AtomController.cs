using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to every atom prefab root.
/// Requires: Rigidbody (kinematic=true), SphereCollider (isTrigger=true),
///           XRGrabInteractable, AtomController
///
/// Valency examples to set in Inspector:
///   H  → maxBonds = 1
///   O  → maxBonds = 2
///   N  → maxBonds = 3
///   C  → maxBonds = 4
/// </summary>
public class AtomController : MonoBehaviour
{
    [Header("Atom Settings")]
    public string atomType = "H";
    public int maxBonds = 1;

     public XRGrabInteractable grabInteractable;
     public List<SnapPoint> snapPoints = new List<SnapPoint>();
     public bool isBeingHeld = false;

    /// <summary>
    /// True ONLY when ALL valency slots are filled (bondCount == maxBonds).
    /// A partially-bonded atom (e.g. C with 2 of 4 bonds used) is NOT locked —
    /// it can still be grabbed and can still accept more bonds.
    /// </summary>
     public bool isLocked = false;

    private List<AtomController> bondedAtoms = new List<AtomController>();

    // ── Accessors ────────────────────────────────────────────────────

    public int BondCount => bondedAtoms.Count;
    public bool CanBond() => bondedAtoms.Count < maxBonds;
    public bool IsSaturated() => bondedAtoms.Count >= maxBonds;
    public bool IsBondedWith(AtomController other) => bondedAtoms.Contains(other);

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        snapPoints.AddRange(GetComponentsInChildren<SnapPoint>());
    }

    private void OnEnable()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.AddListener(_ => isBeingHeld = true);
        grabInteractable.selectExited.AddListener(_ => isBeingHeld = false);
    }

    private void OnDisable()
    {
        if (grabInteractable == null) return;
        grabInteractable.selectEntered.RemoveAllListeners();
        grabInteractable.selectExited.RemoveAllListeners();
    }

    // ── Bond state ───────────────────────────────────────────────────

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

    /// <summary>
    /// Called by BondManager after every successful bond.
    /// Locks the atom ONLY if ALL valency slots are now filled.
    ///
    ///   H  after 1 bond  → locked   (maxBonds = 1, now full)
    ///   O  after 1 bond  → NOT locked (maxBonds = 2, still 1 slot free)
    ///   O  after 2 bonds → locked
    ///   C  after 1 bond  → NOT locked (3 slots still free)
    ///   C  after 4 bonds → locked
    /// </summary>
    public void CheckAndLockIfSaturated()
    {
       // still has open valency — stay unlocked

        isLocked = true;

        // Disable grab so the user can't pull a fully-bonded atom away from
        // its partners. Whole-molecule pickup should live on the molecule prefab.
        if (grabInteractable != null)
            grabInteractable.enabled = false;

        Debug.Log($"[AtomController] '{name}' ({atomType}) saturated ({maxBonds}/{maxBonds}) — locked.");
    }

    // ── Collision — only the HELD atom tries to bond ─────────────────

    private void OnTriggerEnter(Collider other)
    {
        // Fully saturated atoms cannot initiate new bonds
       // if (isLocked) return;

        // Only the atom currently in the player's hand initiates bonding
        if (!isBeingHeld) return;

        AtomController otherAtom = other.GetComponentInParent<AtomController>();
        if (otherAtom == null || otherAtom == this) return;

        // The target atom must still have at least one open valency slot.
        // isLocked is only true when it is fully saturated, so this check
        // correctly allows partially-bonded C/N/O as targets.
       // if (otherAtom.isLocked) return;

        Debug.Log($"[AtomController] '{name}' touched '{otherAtom.name}' — attempting bond");
        BondManager.Instance.TrySnapBond(this, otherAtom);
    }

    private void OnDestroy()
    {
        foreach (var p in new List<AtomController>(bondedAtoms))
            p.RemoveBond(this);
        bondedAtoms.Clear();
    }
}
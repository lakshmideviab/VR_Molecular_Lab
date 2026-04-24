using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Central bond manager — attach to a single GameObject in your scene.
///
/// SNAP RULE (critical):
///   'a' = the HELD atom  (the one the player is dragging — this one moves)
///   'b' = the STATIONARY atom (already placed, possibly already bonded — this NEVER moves)
///
///   We always move 'a' so that a's free SnapPoint lands on b's free SnapPoint.
///   This means Carbon (with H already attached) stays perfectly still while the
///   new H snaps into Carbon's next open slot.
/// </summary>
public class BondManager : MonoBehaviour
{
    public static BondManager Instance;

    [Header("Bond Visual (assign a Cylinder prefab)")]
    public GameObject bondPrefab;

    [Header("Molecule Detection")]
    public MoleculeDatabase database;

    public TextMeshProUGUI Description;
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Symbol;

    [Header("Max bond distance (metres)")]
    public float snapDistance = 0.5f;

    // ─── Internal state ───────────────────────────────────────────────

    private List<AtomController> currentAtoms = new List<AtomController>();
    private List<BondVisual> currentBonds = new List<BondVisual>();

    // ─── Live-updating bond cylinder ─────────────────────────────────

    private class BondVisual
    {
        public GameObject obj;
        public Transform a;
        public Transform b;

        public void Refresh()
        {
            if (obj == null || a == null || b == null) return;
            Vector3 posA = a.position;
            Vector3 posB = b.position;
            float length = Vector3.Distance(posA, posB);
            obj.transform.position = (posA + posB) / 2f;
            obj.transform.up = (posB - posA).normalized;
            obj.transform.localScale = new Vector3(0.04f, length / 2f, 0.04f);
        }
    }

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (bondPrefab == null)
            Debug.LogWarning("[BondManager] bondPrefab not assigned — bonds will be invisible.");
        if (database == null)
            Debug.LogWarning("[BondManager] database not assigned — molecule detection disabled.");
    }

    private void Update()
    {
        foreach (var bv in currentBonds) bv.Refresh();
    }

    // ─────────────────────────────────────────────────────────────────
    // Called by AtomController.OnTriggerEnter
    //   'incoming' = the atom the player is currently holding (it will move)
    //   'anchor'   = the atom already in the scene (it stays still)
    // ─────────────────────────────────────────────────────────────────

    public bool TrySnapBond(AtomController incoming, AtomController anchor)
    {
        if (incoming == null || anchor == null) return false;
        if (incoming.IsBondedWith(anchor)) { Debug.Log("[BondManager] Already bonded"); return false; }
        if (!incoming.CanBond()) { Debug.Log($"[BondManager] {incoming.name} is full"); AudioManager.Instance?.PlayError(); return false; }
        if (!anchor.CanBond()) { Debug.Log($"[BondManager] {anchor.name} is full"); AudioManager.Instance?.PlayError(); return false; }

        float dist = Vector3.Distance(incoming.transform.position, anchor.transform.position);
        if (dist > snapDistance)
        {
            Debug.Log($"[BondManager] Too far: {dist:F2}m > {snapDistance}m");
            return false;
        }

        // ── Snap the INCOMING (held) atom to the ANCHOR's free slot ──
        //
        // We want:   incomingSnapPoint.worldPos  ==  anchorSnapPoint.worldPos
        //
        // Since incomingSnapPoint is a child of 'incoming':
        //   incomingSnap.worldPos = incoming.position + snapOffsetIncoming
        //   (where snapOffsetIncoming = incomingSnap.worldPos - incoming.worldPos,
        //    computed BEFORE we move anything)
        //
        // Solving for the new incoming.position:
        //   incoming.position = anchorSnap.worldPos - snapOffsetIncoming
        //
        // The anchor atom (and everything already bonded to it) is NEVER touched.
        //
        SnapPoint snapAnchor = anchor.GetFreeSnapPoint();
        SnapPoint snapIncoming = incoming.GetFreeSnapPoint();

        if (snapAnchor != null && snapIncoming != null)
        {
            // Capture offset BEFORE moving (snapIncoming is a child, moves with incoming)
            Vector3 snapOffsetIncoming = snapIncoming.transform.position - incoming.transform.position;

            // Move only the incoming atom
            incoming.transform.position = snapAnchor.transform.position - snapOffsetIncoming;

            // Mark both snap points occupied
            snapAnchor.isOccupied = true;
            snapIncoming.isOccupied = true;
            snapAnchor.pairedPoint = snapIncoming;
            snapIncoming.pairedPoint = snapAnchor;
        }
        else
        {
            // No SnapPoints on prefab — place incoming atom directly at anchor's position.
            // This is a fallback; set up SnapPoints on your prefabs for correct placement.
            incoming.transform.position = anchor.transform.position;
            Debug.LogWarning("[BondManager] No SnapPoints found — using centre fallback. Add SnapPoint children to your atom prefabs.");
        }

        // ── Register bond ─────────────────────────────────────────────
        incoming.AddBond(anchor);
        anchor.AddBond(incoming);

        if (!currentAtoms.Contains(incoming)) currentAtoms.Add(incoming);
        if (!currentAtoms.Contains(anchor)) currentAtoms.Add(anchor);

        // ── Lock only when valency is fully used ──────────────────────
        //   H  (maxBonds=1) bonds once  → locked immediately
        //   O  (maxBonds=2) bonds once  → still unlocked, still grabbable
        //   C  (maxBonds=4) bonds 1–3×  → still unlocked
        //   C  (maxBonds=4) bonds 4×    → locked
        incoming.CheckAndLockIfSaturated();
        anchor.CheckAndLockIfSaturated();

        // ── Bond visual ───────────────────────────────────────────────
        var bv = CreateBondVisual(incoming.transform, anchor.transform);
        if (bv != null) currentBonds.Add(bv);

        Debug.Log($"[BondManager] Bonded: {incoming.atomType}({incoming.BondCount}/{incoming.maxBonds}) → {anchor.atomType}({anchor.BondCount}/{anchor.maxBonds})");
        AudioManager.Instance?.PlayBond();
        CheckMolecule();
        return true;
    }

    // ─────────────────────────────────────────────────────────────────

    private BondVisual CreateBondVisual(Transform tA, Transform tB)
    {
        if (bondPrefab == null) return null;
        var bv = new BondVisual { obj = Instantiate(bondPrefab), a = tA, b = tB };
        bv.Refresh();
        return bv;
    }

    // ─────────────────────────────────────────────────────────────────
    // Molecule detection
    // ─────────────────────────────────────────────────────────────────

    private void CheckMolecule()
    {
        if (database == null) return;
        List<string> have = currentAtoms.Select(x => x.atomType).OrderBy(x => x).ToList();

        foreach (var mol in database.molecules)
        {
            if (!have.SequenceEqual(mol.atoms.OrderBy(x => x).ToList())) continue;
            Debug.Log($"[BondManager] Molecule complete: {mol.moleculeName}!");
            CompleteMolecule(mol);
            return;
        }
    }

    private void CompleteMolecule(MoleculeData mol)
    {
        if (mol.moleculePrefab != null)
        {
            Vector3 center = currentAtoms
                .Aggregate(Vector3.zero, (s, a) => s + a.transform.position)
                / currentAtoms.Count;

            Instantiate(mol.moleculePrefab, center, Quaternion.identity);
            Description.text = mol.Description;
            Name.text = mol.moleculeName;
            Symbol.text = mol.symbolName;
            AudioManager.Instance?.PlayMoleculeComplete();
        }

        foreach (var atom in currentAtoms) if (atom != null) Destroy(atom.gameObject);
        foreach (var bv in currentBonds) if (bv.obj != null) Destroy(bv.obj);

        currentAtoms.Clear();
        currentBonds.Clear();
    }

    // ─────────────────────────────────────────────────────────────────

    public void ClearAll()
    {
        foreach (var bv in currentBonds) if (bv.obj != null) Destroy(bv.obj);
        currentAtoms.Clear();
        currentBonds.Clear();
    }

    public void AddBlank()
    {
        Description.text = "";
        Name.text = "";
        Symbol.text = "";
    }
}
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class BondManager : MonoBehaviour
{
    public static BondManager Instance;

    [Header("Bond Visual (assign a Cylinder prefab)")]
    public GameObject bondPrefab;

    [Header("Molecule Detection")]
    public MoleculeDatabase database;

    [Header("Max bond distance (metres)")]
    public float snapDistance = 0.5f;

    [Header("Multi-Bond Visual Settings")]
    [Tooltip("Side offset between parallel cylinders for double/triple bonds (metres)")]
    public float bondOffset = 0.03f;

    [Tooltip("Thickness of each cylinder (local x/z scale)")]
    public float cylinderThickness = 0.025f;

    private static readonly Dictionary<string, int> BondOrderTable = new Dictionary<string, int>
    {
        { "C-N", 3 },
        { "C-O", 2 },
        { "O-O", 2 },
        { "N-N", 3 },
        { "N-O", 2 },
    };

    private static int GetBondOrder(string typeA, string typeB)
    {
        string key = string.Compare(typeA, typeB) <= 0
            ? $"{typeA}-{typeB}"
            : $"{typeB}-{typeA}";

        return BondOrderTable.TryGetValue(key, out int order) ? order : 1;
    }

    private List<AtomController> currentAtoms = new List<AtomController>();
    private List<BondVisualGroup> currentBonds = new List<BondVisualGroup>();
    private List<AtomController> allSpawnedAtoms = new List<AtomController>();
    private List<GameObject> spawnedMolecules = new List<GameObject>();

    private class BondVisualGroup
    {
        public List<GameObject> cylinders = new List<GameObject>();
        public Transform a;
        public Transform b;
        public int order;
        public float offset;
        public float thickness;

        public void Refresh()
        {
            if (a == null || b == null) return;

            Vector3 posA = a.position;
            Vector3 posB = b.position;
            Vector3 axis = posB - posA;
            float length = axis.magnitude;
            if (length < 0.0001f) return;

            Vector3 dir = axis / length;
            Vector3 mid = (posA + posB) * 0.5f;

            Vector3 perp = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) < 0.9f
                ? Vector3.Cross(dir, Vector3.up).normalized
                : Vector3.Cross(dir, Vector3.right).normalized;

            List<Vector3> offsets = GetOffsets(order, perp, offset);

            for (int i = 0; i < cylinders.Count; i++)
            {
                if (cylinders[i] == null) continue;
                cylinders[i].transform.position = mid + offsets[i];
                cylinders[i].transform.up = dir;
                cylinders[i].transform.localScale = new Vector3(thickness, length / 2f, thickness);
            }
        }

        private static List<Vector3> GetOffsets(int order, Vector3 perp, float gap)
        {
            switch (order)
            {
                case 2:
                    return new List<Vector3>
                    {
                        perp * -gap,
                        perp *  gap
                    };
                case 3:
                    return new List<Vector3>
                    {
                        Vector3.zero,
                        perp * -gap,
                        perp *  gap
                    };
                default:
                    return new List<Vector3> { Vector3.zero };
            }
        }

        public void DestroyAll()
        {
            foreach (var c in cylinders)
                if (c != null) Object.Destroy(c);
            cylinders.Clear();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (bondPrefab == null)
            Debug.LogWarning("[BondManager] bondPrefab not assigned — bonds invisible.");
        if (database == null)
            Debug.LogWarning("[BondManager] database not assigned — molecule detection disabled.");
    }

    private void Update()
    {
        foreach (var bg in currentBonds) bg.Refresh();
    }

    public void RegisterAtom(AtomController atom)
    {
        if (atom != null && !allSpawnedAtoms.Contains(atom))
            allSpawnedAtoms.Add(atom);
    }

    public bool TrySnapBond(AtomController incoming, AtomController anchor)
    {
        if (incoming == null || anchor == null) return false;
        if (incoming.IsBondedWith(anchor)) { Debug.Log("[BondManager] Already bonded"); return false; }
        if (!incoming.CanBond()) return false;
        if (!anchor.CanBond()) return false;

        float dist = Vector3.Distance(incoming.transform.position, anchor.transform.position);
        if (dist > snapDistance) return false;

        float radiusAnchor = GetAtomRadius(anchor);
        float radiusIncoming = GetAtomRadius(incoming);
        float bondLength = radiusAnchor + radiusIncoming;

        SnapPoint snapAnchor = anchor.GetFreeSnapPoint();
        SnapPoint snapIncoming = incoming.GetFreeSnapPoint();

        if (snapAnchor != null && snapIncoming != null)
        {
            Vector3 bondDir = (snapAnchor.transform.position - anchor.transform.position);
            if (bondDir == Vector3.zero) bondDir = Vector3.right;
            bondDir.Normalize();

            incoming.transform.position = anchor.transform.position + bondDir * bondLength;

            Vector3 incomingSnapLocal = snapIncoming.transform.position - incoming.transform.position;
            if (incomingSnapLocal != Vector3.zero)
            {
                Quaternion rot = Quaternion.FromToRotation(incomingSnapLocal.normalized, -bondDir);
                incoming.transform.rotation = rot * incoming.transform.rotation;
            }

            snapAnchor.isOccupied = true;
            snapIncoming.isOccupied = true;
            snapAnchor.pairedPoint = snapIncoming;
            snapIncoming.pairedPoint = snapAnchor;
        }
        else
        {
            Vector3 dir = incoming.transform.position - anchor.transform.position;
            if (dir == Vector3.zero) dir = Vector3.right;
            incoming.transform.position = anchor.transform.position + dir.normalized * bondLength;
        }

        SeparateOverlap(incoming, anchor);

        incoming.AddBond(anchor);
        anchor.AddBond(incoming);

        if (!currentAtoms.Contains(incoming)) currentAtoms.Add(incoming);
        if (!currentAtoms.Contains(anchor)) currentAtoms.Add(anchor);

        incoming.CheckAndLockIfSaturated();
        anchor.CheckAndLockIfSaturated();

        if (!incoming.isLocked) incoming.RefreshMaterial();
        if (!anchor.isLocked) anchor.RefreshMaterial();

        if (incoming.grabInteractable != null) incoming.grabInteractable.enabled = false;
        if (anchor.grabInteractable != null) anchor.grabInteractable.enabled = false;

        int bondOrder = GetBondOrder(incoming.atomType, anchor.atomType);
        var group = CreateBondVisualGroup(incoming.transform, anchor.transform, bondOrder);
        if (group != null) currentBonds.Add(group);

        Debug.Log($"[BondManager] Bond order {bondOrder}: {incoming.atomType}({incoming.BondCount}/{incoming.maxBonds}) -> {anchor.atomType}({anchor.BondCount}/{anchor.maxBonds})");
        AudioManager.Instance?.PlayBond();
        CheckMolecule();
        return true;
    }

    private BondVisualGroup CreateBondVisualGroup(Transform tA, Transform tB, int order)
    {
        if (bondPrefab == null) return null;

        var group = new BondVisualGroup
        {
            a = tA,
            b = tB,
            order = order,
            offset = bondOffset,
            thickness = cylinderThickness
        };

        for (int i = 0; i < order; i++)
            group.cylinders.Add(Instantiate(bondPrefab));

        group.Refresh();
        return group;
    }

    private void SeparateOverlap(AtomController incoming, AtomController anchor)
    {
        float minDist = GetAtomRadius(incoming) + GetAtomRadius(anchor);
        Vector3 delta = incoming.transform.position - anchor.transform.position;
        float dist = delta.magnitude;

        if (dist < minDist)
        {
            Vector3 dir = dist > 0.0001f ? delta / dist : Vector3.right;
            incoming.transform.position = anchor.transform.position + dir * minDist;
        }
    }

    private float GetAtomRadius(AtomController atom)
    {
        var sc = atom.GetComponent<SphereCollider>();
        if (sc != null)
            return sc.radius * Mathf.Max(
                atom.transform.lossyScale.x,
                atom.transform.lossyScale.y,
                atom.transform.lossyScale.z);
        return Mathf.Max(
            atom.transform.lossyScale.x,
            atom.transform.lossyScale.y,
            atom.transform.lossyScale.z) * 0.5f;
    }

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
            spawnedMolecules.Add(Instantiate(mol.moleculePrefab, center, Quaternion.identity));
        }

        MoleculeUIManager.Instance?.ShowPopup(mol.moleculeName, mol.symbolName, mol.Description);
        AudioManager.Instance?.PlayMoleculeComplete();

        foreach (var atom in currentAtoms)
        {
            allSpawnedAtoms.Remove(atom);
            if (atom != null) Destroy(atom.gameObject);
        }

        foreach (var bg in currentBonds) bg.DestroyAll();

        currentAtoms.Clear();
        currentBonds.Clear();
    }

    public void ClearMolecules()
    {
        foreach (var mol in spawnedMolecules)
            if (mol != null) Destroy(mol);
        spawnedMolecules.Clear();
        MoleculeUIManager.Instance?.HidePopup();
    }

    public void ClearAll()
    {
        foreach (var bg in currentBonds) bg.DestroyAll();
        foreach (var atom in allSpawnedAtoms) if (atom != null) Destroy(atom.gameObject);
        foreach (var mol in spawnedMolecules) if (mol != null) Destroy(mol);

        currentAtoms.Clear();
        currentBonds.Clear();
        allSpawnedAtoms.Clear();
        spawnedMolecules.Clear();

        MoleculeUIManager.Instance?.HidePopup();
        Debug.Log("[BondManager] Full scene cleared.");
    }

    public void AddBlank() => MoleculeUIManager.Instance?.HidePopup();
}
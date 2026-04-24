using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject — holds all known molecules.
/// Create: right-click in Project > Create > Molecule > Molecule Database
/// </summary>
[CreateAssetMenu(fileName = "MoleculeDatabase", menuName = "Molecule/Molecule Database")]
public class MoleculeDatabase : ScriptableObject
{
    public List<MoleculeData> molecules = new List<MoleculeData>();
}

/// <summary>
/// Data for one molecule. Order of atoms doesn't matter — BondManager sorts before comparing.
/// Example: Water = atoms [H, H, O]
/// </summary>
[System.Serializable]
public class MoleculeData
{
    public string moleculeName;
    public string Description;
    public string symbolName;

    [Tooltip("Atom types in this molecule e.g. H, H, O for water")]
    public List<string> atoms = new List<string>();

    [Tooltip("Optional: prefab spawned when molecule completes. Atoms are destroyed after.")]
    public GameObject moleculePrefab;
}
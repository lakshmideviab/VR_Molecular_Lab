using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoleculeDatabase", menuName = "Molecule/Molecule Database")]
public class MoleculeDatabase : ScriptableObject
{
    public List<MoleculeData> molecules = new List<MoleculeData>();
}
[System.Serializable]
public class MoleculeData
{
    public string moleculeName;
    public string Description;
    public string symbolName;
    public List<string> atoms = new List<string>();
    public GameObject moleculePrefab;
}
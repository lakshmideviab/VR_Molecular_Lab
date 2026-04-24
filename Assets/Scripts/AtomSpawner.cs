using UnityEngine;

/// <summary>
/// Spawns atom prefabs at a random position inside a defined world-space box zone.
/// The zone is defined by two Transform points: spawnZoneMin and spawnZoneMax.
/// Create two empty GameObjects in the scene, position them at opposite corners
/// of the desired spawn box, then drag them into these fields in the Inspector.
/// </summary>
public class AtomSpawner : MonoBehaviour
{
    public static AtomSpawner Instance;

    [System.Serializable]
    public class AtomEntry
    {
        public string atomType;
        public GameObject prefab;
    }

    [Header("Atom Prefabs")]
    public AtomEntry[] atomEntries;

    [Header("Spawn Zone")]
    [Tooltip("One corner of the spawn box. Place an empty GameObject at this position in the scene.")]
    public Transform spawnZoneMin;

    [Tooltip("Opposite corner of the spawn box. Place an empty GameObject at this position in the scene.")]
    public Transform spawnZoneMax;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Returns a random position inside the box defined by spawnZoneMin and spawnZoneMax.
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 min = spawnZoneMin.position;
        Vector3 max = spawnZoneMax.position;

        return new Vector3(
            Random.Range(Mathf.Min(min.x, max.x), Mathf.Max(min.x, max.x)),
            Random.Range(Mathf.Min(min.y, max.y), Mathf.Max(min.y, max.y)),
            Random.Range(Mathf.Min(min.z, max.z), Mathf.Max(min.z, max.z))
        );
    }

    /// <summary>
    /// Spawns an atom of the given type at a random position inside the spawn zone.
    /// Called by AtomSpawnButton.
    /// </summary>
    public GameObject SpawnAtom(string type)
    {
        AtomEntry entry = System.Array.Find(atomEntries, e => e.atomType == type);
        if (entry == null)
        {
            Debug.LogWarning($"AtomSpawner: No prefab registered for type '{type}'");
            return null;
        }

        if (spawnZoneMin == null || spawnZoneMax == null)
        {
            Debug.LogError("AtomSpawner: spawnZoneMin or spawnZoneMax is not assigned in the Inspector.");
            return null;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject atom = Instantiate(entry.prefab, spawnPos, Quaternion.identity);

        AtomController ctrl = atom.GetComponent<AtomController>();
        if (ctrl != null)
            ctrl.atomType = type;

        return atom;
    }

    /// <summary>
    /// Draws the spawn zone as a visible yellow wireframe box in the Scene view.
    /// Only visible in the editor — no runtime cost.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (spawnZoneMin == null || spawnZoneMax == null) return;

        Vector3 min = spawnZoneMin.position;
        Vector3 max = spawnZoneMax.position;

        Vector3 center = (min + max) / 2f;
        Vector3 size = new Vector3(
            Mathf.Abs(max.x - min.x),
            Mathf.Abs(max.y - min.y),
            Mathf.Abs(max.z - min.z)
        );

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}
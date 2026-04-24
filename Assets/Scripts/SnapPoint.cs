using UnityEngine;

/// <summary>
/// Place as child GameObjects on each atom to mark bonding points.
///
/// SETUP TIP: Position each SnapPoint child at the surface of the atom sphere,
/// pointing outward (e.g. for Hydrogen place it at local (0, 0.5, 0) and
/// rotate it so its blue Z-axis faces away from the atom centre).
/// BondManager will move atom B so that its SnapPoint lands exactly on
/// atom A's SnapPoint when a bond forms.
/// </summary>
public class SnapPoint : MonoBehaviour
{
    [HideInInspector] public bool isOccupied = false;
    [HideInInspector] public AtomController ownerAtom;
    [HideInInspector] public SnapPoint pairedPoint;

    private void Awake()
    {
        ownerAtom = GetComponentInParent<AtomController>();
    }

    public void Release()
    {
        if (pairedPoint != null)
        {
            pairedPoint.isOccupied = false;
            pairedPoint.pairedPoint = null;
        }
        isOccupied = false;
        pairedPoint = null;
    }

    // Shows cyan sphere in editor so you can see where snap points are
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.03f);
    }
}
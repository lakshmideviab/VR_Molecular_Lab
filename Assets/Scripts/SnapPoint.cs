using UnityEngine;

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


    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.03f);
    }
}
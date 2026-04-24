using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class AtomSpawnButton : MonoBehaviour
{
    
    public string atomType = "H";

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        GetComponent<Button>().onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        if (AtomSpawner.Instance == null)
        {
            Debug.LogError("[AtomSpawnButton] No AtomSpawner in scene!");
            return;
        }

        GameObject spawnedAtom = AtomSpawner.Instance.SpawnAtom(atomType);

        if (spawnedAtom != null)
        {
            AtomController ac = spawnedAtom.GetComponent<AtomController>();
            BondManager.Instance?.RegisterAtom(ac);
        }

        AudioManager.Instance?.PlayUIClick();
    }
}
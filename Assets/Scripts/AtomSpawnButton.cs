using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a UI Button. Set atomType in Inspector.
/// Calls AtomSpawner.Instance.SpawnAtom() when clicked.
/// Make sure you have an AtomSpawner in the scene!
/// </summary>
[RequireComponent(typeof(Button))]
public class AtomSpawnButton : MonoBehaviour
{
    [Tooltip("Must match the atomType string on your atom prefab (e.g. H, O, C)")]
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
            Debug.LogError("[AtomSpawnButton] No AtomSpawner in scene! Add one.");
            return;
        }
        AtomSpawner.Instance.SpawnAtom(atomType);
        AudioManager.Instance?.PlayUIClick();
    }
}
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Source")]
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip grabSound;
    public AudioClip bondSound;
    public AudioClip moleculeCompleteSound;
    public AudioClip uiClickSound;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
    
    public void PlayGrab() => PlaySound(grabSound);
    public void PlayBond() => PlaySound(bondSound);
    public void PlayMoleculeComplete() => PlaySound(moleculeCompleteSound);
    public void PlayUIClick() => PlaySound(uiClickSound);
   
}
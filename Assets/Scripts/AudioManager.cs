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
    public AudioClip errorSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 🔊 Play generic sound
    public void PlaySound(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // 🔊 Specific helpers (clean calls)
    public void PlayGrab() => PlaySound(grabSound);
    public void PlayBond() => PlaySound(bondSound);
    public void PlayMoleculeComplete() => PlaySound(moleculeCompleteSound);
    public void PlayUIClick() => PlaySound(uiClickSound);
    public void PlayError() => PlaySound(errorSound);
}
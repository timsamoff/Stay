using UnityEngine;
using System.Collections; // Required for IEnumerator

[RequireComponent(typeof(AudioSource))]
public class AudioRandomizer : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float delayBeforePlay = 2f;
    [SerializeField] private bool loop = false;
    [SerializeField][Range(0f, 1f)] private float volume = 1f;
    [SerializeField][Range(0.1f, 3f)] private float pitchMin = 1f;
    [SerializeField][Range(0.1f, 3f)] private float pitchMax = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = loop;
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(PlayWithDelay());
        }
    }

    private IEnumerator PlayWithDelay()
    {
        if (delayBeforePlay > 0f)
        {
            yield return new WaitForSeconds(delayBeforePlay);
        }
        PlayRandomClip();
    }

    public void PlayRandomClip()
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("No audio clips assigned to AudioRandomizer", this);
            return;
        }

        int randomIndex = Random.Range(0, clips.Length);
        audioSource.clip = clips[randomIndex];
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.Play();
    }

    public void PlayRandomClipWithDelay(float customDelay)
    {
        StartCoroutine(PlayWithCustomDelay(customDelay));
    }

    private IEnumerator PlayWithCustomDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        PlayRandomClip();
    }

    public void PlayRandomClipFromArray(AudioClip[] customClips)
    {
        if (customClips == null || customClips.Length == 0)
        {
            Debug.LogWarning("No custom audio clips provided", this);
            return;
        }

        int randomIndex = Random.Range(0, customClips.Length);
        audioSource.clip = customClips[randomIndex];
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.Play();
    }
}
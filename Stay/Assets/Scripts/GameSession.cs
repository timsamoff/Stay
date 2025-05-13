using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSession : MonoBehaviour
{
    [Header("Screen Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string winScene = "Win";
    [SerializeField] private string loseScene = "Lose";

    [Header("Music Fade")]
    [SerializeField] private float musicFadeTime = 2f;
    [SerializeField][Range(0, 1)] private float musicMaxVolume = 0.25f;
    private AudioSource backgroundMusic;

    private void Awake()
    {
        backgroundMusic = GetComponent<AudioSource>();
        backgroundMusic.volume = 0f; // Start silent
        fadeCanvasGroup.alpha = 1f; // Start black
        fadeCanvasGroup.gameObject.SetActive(true);
    }

    private void Start()
    {
        StartCoroutine(FadeAudio(0f, musicMaxVolume, musicFadeTime)); // Use musicMaxVolume
        StartCoroutine(FadeVisuals(1f, 0f, fadeDuration));
    }

    public void TriggerWin() => StartCoroutine(EndGame(winScene));
    public void TriggerLoss() => StartCoroutine(EndGame(loseScene));

    private IEnumerator EndGame(string sceneName)
    {
        StartCoroutine(FadeAudio(backgroundMusic.volume, 0f, musicFadeTime));
        yield return StartCoroutine(FadeVisuals(0f, 1f, fadeDuration));
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeAudio(float startVol, float endVol, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            backgroundMusic.volume = Mathf.Lerp(startVol, endVol, elapsed / duration);
            yield return null;
        }
        backgroundMusic.volume = endVol;
    }

    private IEnumerator FadeVisuals(float startAlpha, float endAlpha, float duration)
    {
        fadeCanvasGroup.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = endAlpha;
    }
}
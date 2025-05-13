using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string startSceneName = "StayMain";
    [SerializeField] private string aboutSceneName = "About";
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Audio Settings")]
    [SerializeField] private AudioClip buttonHover;
    [SerializeField] private AudioClip buttonClick;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float soundCooldown = 0.1f; // Minimum time between sounds

    private float lastHoverTime = -1f;
    private float lastClickTime = -1f;

    private void Start()
    {
        if (fadeCanvas == null)
            fadeCanvas = GetComponent<CanvasGroup>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        fadeCanvas.gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    public void PlayButtonHoverSound()
    {
        if (buttonHover != null && audioSource != null && Time.time > lastHoverTime + soundCooldown)
        {
            audioSource.PlayOneShot(buttonHover, 0.1f);
            lastHoverTime = Time.time;
        }
    }

    public void PlayButtonClickSound()
    {
        if (buttonClick != null && audioSource != null && Time.time > lastClickTime + soundCooldown)
        {
            audioSource.PlayOneShot(buttonClick, 1.0f);
            lastClickTime = Time.time;
        }
    }

    public void StartGame()
    {
        PlayButtonClickSound();
        StartCoroutine(FadeOutAndLoadScene(startSceneName));
    }

    public void About()
    {
        PlayButtonClickSound();
        StartCoroutine(FadeOutAndLoadScene(aboutSceneName));
    }

    public void Menu()
    {
        PlayButtonClickSound();
        StartCoroutine(FadeOutAndLoadScene(menuSceneName));
    }

    private IEnumerator FadeIn()
    {
        fadeCanvas.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.SmoothStep(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false;
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        fadeCanvas.blocksRaycasts = true;
        float elapsed = 0f;
        float startAlpha = fadeCanvas.alpha;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / fadeDuration);
            yield return null;
        }
        SceneManager.LoadScene(sceneName);
    }
}
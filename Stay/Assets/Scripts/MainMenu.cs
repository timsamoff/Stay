using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string startSceneName = "StayMain";
    [SerializeField] private string aboutSceneName = "About";
    [SerializeField] private string menuSceneName = "MainMenu";

    private void Start()
    {
        if (fadeCanvas == null)
        {
            fadeCanvas = GetComponent<CanvasGroup>();
        }

        fadeCanvas.gameObject.SetActive(true);

        StartCoroutine(FadeIn());
    }

    public void StartGame()
    {
        StartCoroutine(FadeOutAndLoadScene(startSceneName));
    }

    public void About()
    {
        StartCoroutine(FadeOutAndLoadScene(aboutSceneName));
    }

    public void Menu()
    {
        StartCoroutine(FadeOutAndLoadScene(menuSceneName));
    }

    private IEnumerator FadeIn()
    {
        fadeCanvas.blocksRaycasts = true;  // Block input during fade

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.SmoothStep(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = 0f;
        fadeCanvas.interactable = true;
        fadeCanvas.blocksRaycasts = false;
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        fadeCanvas.blocksRaycasts = true; // Block input during fade

        float elapsed = 0f;
        float startAlpha = fadeCanvas.alpha;
        float targetAlpha = 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;

        SceneManager.LoadScene(sceneName);
    }
}
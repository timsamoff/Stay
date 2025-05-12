using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string menuScene = "MainMenu";

    private bool isFading;
    private bool gameOver;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        // Fade in to start game
        StartCoroutine(FadeRoutine(1f, 0f, true));
    }

    public void TriggerWin() => EndGame();
    public void TriggerLoss() => EndGame();

    private void EndGame()
    {
        if (gameOver || isFading) return;
        gameOver = true;
        StartCoroutine(FadeRoutine(0f, 1f, false));
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, bool disableAfter)
    {
        isFading = true;
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true; // Block interactions during fade

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
        fadeCanvasGroup.blocksRaycasts = false;

        if (disableAfter)
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            SceneManager.LoadScene(menuScene);
        }

        isFading = false;
    }
}
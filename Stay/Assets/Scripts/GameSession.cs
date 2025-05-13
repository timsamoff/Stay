using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string winScene = "Win";
    [SerializeField] private string loseScene = "Lose";

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Start with screen black and fade in
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;
        StartCoroutine(Fade(1f, 0f)); // Fade from black to clear
    }

    public void TriggerWin()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToScene(winScene));
    }

    public void TriggerLoss()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionToScene(loseScene));
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f));

        // Load scene
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = (to == 1f);
    }
}
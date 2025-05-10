using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private string menuScene = "MainMenu";

    [Header("Hand Tracking")]
    [SerializeField] private Transform otherHandTransform;

    [HideInInspector] public bool gameOver = false;
    private Vector3 initialOtherHandPosition;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        fadeCanvas.gameObject.SetActive(true);
        initialOtherHandPosition = otherHandTransform.position;
        StartCoroutine(FadeOutAndReturn("Scene Started"));
    }

    public void TriggerWin()
    {
        if (gameOver) return;
        gameOver = true;
        StartCoroutine(FadeOutAndReturn("Connection made"));
    }

    public void TriggerLoss(string reason)
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("Game Over: " + reason);
        StartCoroutine(FadeOutAndReturn(reason));
    }

    private IEnumerator FadeOutAndReturn(string reason)
    {
        fadeCanvas.interactable = false;
        fadeCanvas.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.SmoothStep(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvas.interactable = true;
        fadeCanvas.blocksRaycasts = true;
        SceneManager.LoadScene(menuScene);
    }
}
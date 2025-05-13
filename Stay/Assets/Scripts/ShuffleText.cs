using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ShuffleText : MonoBehaviour
{
    [Header("Text Options")]
    [SerializeField] private TextMeshProUGUI _textComponent;
    [SerializeField]
    [TextArea(1, 3)]
    private string[] _textOptions =
    {
        "Default text option 1",
        "Default text option 2",
        "Default text option 3"
    };
    

    private List<string> _shuffleBag;
    private string _lastUsedText;

    private void Start()
    {
        if (_textOptions == null || _textOptions.Length == 0)
        {
            Debug.LogWarning($"{nameof(ShuffleText)}: No text options assigned", this);
            return;
        }

        if (_textComponent == null)
        {
            Debug.LogWarning($"{nameof(ShuffleText)}: No TextMeshPro component assigned", this);
            return;
        }

        InitializeShuffleBag();
        DisplayRandomText();
    }

    private void InitializeShuffleBag()
    {
        _shuffleBag = new List<string>(_textOptions);

        // Remove last used text
        if (!string.IsNullOrEmpty(_lastUsedText) && _shuffleBag.Contains(_lastUsedText))
        {
            _shuffleBag.Remove(_lastUsedText);
        }

        // Fisher-Yates shuffle
        for (int i = _shuffleBag.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (_shuffleBag[i], _shuffleBag[randomIndex]) = (_shuffleBag[randomIndex], _shuffleBag[i]);
        }
    }

    private void DisplayRandomText()
    {
        if (_shuffleBag.Count == 0)
        {
            Debug.LogWarning($"{nameof(ShuffleText)}: Shuffle bag is empty", this);
            return;
        }

        // Get next text
        string nextText = _shuffleBag[0];
        _shuffleBag.RemoveAt(0);
        _lastUsedText = nextText;

        // Apply to text component
        _textComponent.text = nextText;
    }

#if UNITY_EDITOR
    // Editor-only validation
    private void OnValidate()
    {
        if (_textComponent == null)
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
        }
    }
#endif
}
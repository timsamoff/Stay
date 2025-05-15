using UnityEngine;

public class MultiResolutionCursor : MonoBehaviour
{
    [Header("Cursor Textures")]
    [SerializeField] private Texture2D lowResCursorTexture;
    [SerializeField] private Texture2D midResCursorTexture;
    [SerializeField] private Texture2D highResCursorTexture;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SetCursorBasedOnResolution();
    }

    void SetCursorBasedOnResolution()
    {
        int screenWidth = Screen.width;
        Texture2D cursorToSet;

        if (screenWidth <= 1366) // Low resolution
        {
            cursorToSet = lowResCursorTexture;
        }
        else if (screenWidth <= 1920) // Medium resolution
        {
            cursorToSet = midResCursorTexture;
        }
        else // High resolution
        {
            cursorToSet = highResCursorTexture;
        }

        Vector2 hotspot = new Vector2(cursorToSet.width / 2, cursorToSet.height / 2);
        Cursor.SetCursor(cursorToSet, hotspot, CursorMode.ForceSoftware);
    }
}
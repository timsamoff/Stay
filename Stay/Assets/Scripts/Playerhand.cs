using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float smoothing = 0.1f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Camera mainCamera;

    private Vector3 velocity;
    private float currentSpeed;
    private float maxAllowedY; // The highest Y position where hand bottom touches screen bottom
    private float handBottomOffset;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Calculate the hand's bottom offset from its pivot
        handBottomOffset = transform.position.y - spriteRenderer.bounds.min.y;

        // Calculate maximum allowed Y position (when hand bottom touches screen bottom)
        float screenBottomWorldY = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(mainCamera.transform.position.z))).y;
        maxAllowedY = screenBottomWorldY + handBottomOffset;

        Cursor.visible = false;

        Debug.Log($"Hand initialized - Bottom Offset: {handBottomOffset} | Max Allowed Y: {maxAllowedY}");
    }

    private void Update()
    {
        // Get input direction
        Vector3 inputDirection = new Vector3(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y"),
            0
        ).normalized;

        // Calculate target velocity
        Vector3 targetVelocity = inputDirection * speed;

        // Smooth the velocity
        velocity = Vector3.Lerp(velocity, targetVelocity, smoothing * Time.deltaTime * 60f);

        // Calculate new position
        Vector3 newPosition = transform.position + velocity * Time.deltaTime;

        // Calculate hand's bottom position at new position
        float newHandBottom = newPosition.y - handBottomOffset;

        // CONSTRAINT: Prevent moving up when hand bottom is at screen bottom
        if (newHandBottom >= maxAllowedY - handBottomOffset && velocity.y > 0)
        {
            newPosition.y = maxAllowedY;
            velocity.y = 0; // Stop upward movement
        }

        // Apply movement
        transform.position = newPosition;
        currentSpeed = velocity.magnitude;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw hand bottom position
        Gizmos.color = Color.green;
        Vector3 handBottomPos = new Vector3(
            transform.position.x,
            transform.position.y - handBottomOffset,
            transform.position.z
        );
        Gizmos.DrawSphere(handBottomPos, 0.1f);

        // Draw screen bottom
        Gizmos.color = Color.red;
        Vector3 screenBottomStart = new Vector3(-10, maxAllowedY - handBottomOffset, 0);
        Vector3 screenBottomEnd = new Vector3(10, maxAllowedY - handBottomOffset, 0);
        Gizmos.DrawLine(screenBottomStart, screenBottomEnd);
    }
}
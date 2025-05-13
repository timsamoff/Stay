using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float smoothing = 0.1f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Camera mainCamera;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] movementSounds;
    [SerializeField] private float soundCooldown = 0.1f;
    [SerializeField] private float minSpeedForSound = 1f;
    [SerializeField] private AudioSource audioSource;

    private Vector3 velocity;
    private float currentSpeed;
    private float maxAllowedY;
    private float handBottomOffset;
    private float lastSoundTime;
    private Vector3 lastPosition;
    private bool movementEnabled = true;

    public bool MovementEnabled
    {
        get => movementEnabled;
        set
        {
            // Only trigger changes when the state actually changes
            if (movementEnabled != value)
            {
                movementEnabled = value;
                if (!value)
                {
                    velocity = Vector3.zero;
                    currentSpeed = 0f;
                    Debug.Log("PlayerHand movement disabled");
                }
                else
                {
                    Debug.Log("PlayerHand movement enabled");
                }
            }
        }
    }

    private void Start()
    {
        InitializeComponents();
        CalculateHandBounds();
        Cursor.visible = false;
    }

    private void InitializeComponents()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void CalculateHandBounds()
    {
        handBottomOffset = transform.position.y - spriteRenderer.bounds.min.y;
        float screenBottomWorldY = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(mainCamera.transform.position.z))).y;
        maxAllowedY = screenBottomWorldY + handBottomOffset;
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!MovementEnabled)
        {
            velocity = Vector3.zero;
            currentSpeed = 0f;
            return;
        }

        HandleMovementInput();
        TryPlayMovementSound();
    }

    private void HandleMovementInput()
    {
        Vector3 previousPosition = transform.position;
        Vector3 inputDirection = new Vector3(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y"),
            0
        ).normalized;

        Vector3 targetVelocity = inputDirection * speed;
        velocity = Vector3.Lerp(velocity, targetVelocity, smoothing * Time.deltaTime * 60f);

        Vector3 newPosition = transform.position + velocity * Time.deltaTime;
        newPosition = ApplyVerticalBounds(newPosition);

        transform.position = newPosition;
        currentSpeed = velocity.magnitude;
    }

    private Vector3 ApplyVerticalBounds(Vector3 newPosition)
    {
        float newHandBottom = newPosition.y - handBottomOffset;
        if (newHandBottom >= maxAllowedY - handBottomOffset && velocity.y > 0)
        {
            newPosition.y = maxAllowedY;
            velocity.y = 0;
        }
        return newPosition;
    }

    private void TryPlayMovementSound()
    {
        if (movementSounds == null || movementSounds.Length == 0 || currentSpeed < minSpeedForSound)
            return;

        if (Vector3.Distance(lastPosition, transform.position) > 0.01f &&
            Time.time - lastSoundTime > soundCooldown)
        {
            PlayRandomSound();
            lastSoundTime = Time.time;
        }
        lastPosition = transform.position;
    }

    private void PlayRandomSound()
    {
        if (movementSounds.Length == 0 || audioSource == null)
            return;

        AudioClip clip = movementSounds[Random.Range(0, movementSounds.Length)];
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public float GetCurrentSpeed() => currentSpeed;

    private void OnDisable()
    {
        Cursor.visible = true;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        Vector3 handBottomPos = new Vector3(
            transform.position.x,
            transform.position.y - handBottomOffset,
            transform.position.z
        );
        Gizmos.DrawSphere(handBottomPos, 0.1f);

        Gizmos.color = Color.red;
        Vector3 screenBottomStart = new Vector3(-10, maxAllowedY - handBottomOffset, 0);
        Vector3 screenBottomEnd = new Vector3(10, maxAllowedY - handBottomOffset, 0);
        Gizmos.DrawLine(screenBottomStart, screenBottomEnd);
    }
}
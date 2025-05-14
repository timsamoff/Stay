using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class OtherHand : MonoBehaviour
{
    [Header("Proximity Radii")]
    [SerializeField] private float personalSpaceRadius = 6f;
    [SerializeField] private float recoilSpaceRadius = 3f;
    [SerializeField] private float coverSpaceRadius = 1f;
    [SerializeField] private Vector2 shrinkSpeedRange = new Vector2(0.1f, 0.5f);

    private float shrinkSpeed;

    [Header("Vertical Offsets")]
    [SerializeField] private float recoilSpaceVerticalOffset = 0f;
    [SerializeField] private float coverSpaceVerticalOffset = 0f;

    [Header("Movement Settings")]
    [SerializeField] private float pushSpeed = 5f;
    [SerializeField] private float returnSpeed = 2f;
    [SerializeField] private float recoilPushMultiplier = 2f;
    [SerializeField] private float movementSmoothing = 0.1f;
    [SerializeField] private float maxDistanceFromOrigin = 10f;

    [Header("Movement Space Settings")]
    [SerializeField] private float otherHandApproachSpeed = 1.0f;
    [SerializeField] private float movementSpaceShrinkSpeed = 0.5f;
    [SerializeField] private float returnToOriginSpeed = 2.0f;
    [SerializeField] private string defaultSortingLayer = "BackHand";
    [SerializeField] private string coveringSortingLayer = "CoverHand";
    [SerializeField] private Color movementSpaceColor = new Color(1, 0, 1, 0.5f);
    [SerializeField] private float coverSpaceContactOffset = 0.1f;

    private float currentEffectivePushSpeed;

    [Header("Timing")]
    [SerializeField] private float movementDelay = 2f;

    [Header("References")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private LayerMask playerHandLayer;

    [Header("Collision Bounds")]
    [SerializeField] private float otherHandWidth = 1f;
    private CircleCollider2D playerHandCollider;
    private float playerLeftBound;

    [Header("Win Condition")]
    [SerializeField] private float movementStopDelay = 1f;
    [SerializeField] private float winDelay = 3f;
    [SerializeField] private GameSession gameSession;

    private bool playerHandStopped = false;
    private float coverTime = 0f;
    private bool isCoveringCoverSpace = false;

    [Header("Loss Condition")]
    [SerializeField] private float lossAnimationDuration = 2f;

    private bool isLossTriggered = false;

    [Header("Recoil Sounds")]
    [SerializeField] private AudioClip[] recoilSounds;
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool visualizeColliders = true;
    [SerializeField] private bool drawCollidersInEditor = true;
    [SerializeField] private bool continuousDetection = true;
    [SerializeField] private float detectionInterval = 0.1f;

    // Colliders
    private CircleCollider2D personalSpace;
    private CircleCollider2D recoilSpace;
    private BoxCollider2D coverSpace;
    private CircleCollider2D movementSpace;

    // Movement
    private Vector2 originalPosition;
    private Vector2 targetPosition;
    private Vector2 currentVelocity;
    private bool isInPersonalSpace;
    private bool isInRecoilSpace;
    private Rigidbody2D playerHandRb;

    // States
    private float originalPersonalRadius;
    private bool shrinking = true;
    private bool movementShrinking = false;
    private float movementDelayTimer = 0f;
    private bool movementPending = false;
    private float detectionTimer = 0f;
    private readonly Collider2D[] overlapResults = new Collider2D[5];
    private ContactFilter2D contactFilter;

    // Visuals
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer movementSpaceVisual;
    private Vector2 movementSpaceOriginalPosition;
    private bool isMovingTowardPlayer = false;
    private float originalMovementRadius;
    private bool hasReachedPlayer = false;

    private void Awake()
    {
        contactFilter = new ContactFilter2D().NoFilter();
        originalPersonalRadius = personalSpaceRadius;
        originalPosition = transform.position;
        targetPosition = originalPosition;

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        shrinkSpeed = Random.Range(shrinkSpeedRange.x, shrinkSpeedRange.y);
        Debug.Log($"Shrink speed: {shrinkSpeed}");

        isInPersonalSpace = false;
        isInRecoilSpace = false;

        playerHandCollider = playerHand.GetComponent<CircleCollider2D>();
        if (playerHandCollider == null)
        {
            Debug.LogError("PlayerHand must have a CircleCollider2D component!");
            return;
        }

        playerHandRb = playerHand.GetComponent<Rigidbody2D>();

        CalculatePlayerColliderBounds();

        currentEffectivePushSpeed = pushSpeed;

        CreateProximityCollider(ref personalSpace, personalSpaceRadius, "PersonalSpace");
        CreateProximityCollider(ref recoilSpace, recoilSpaceRadius, "RecoilSpace", recoilSpaceVerticalOffset);
        CreateBoxProximityCollider(ref coverSpace, coverSpaceRadius, "CoverSpace", coverSpaceVerticalOffset);

        coverSpace.enabled = true;
        personalSpace.enabled = true;
        recoilSpace.enabled = true;

        if (gameSession == null)
        {
            gameSession = FindFirstObjectByType<GameSession>();
            if (gameSession == null)
            {
                Debug.LogWarning("GameSession reference not found in scene!");
            }
        }
    }

    private void Update()
    {
        if (isLossTriggered) return;

        HandleShrinkingAnimation();
        CalculatePlayerColliderBounds();

        // Cover space contact
        if (coverSpace != null && coverSpace.enabled)
        {
            int hits = Physics2D.OverlapCollider(coverSpace, contactFilter, overlapResults);
            isCoveringCoverSpace = false;

            for (int i = 0; i < hits; i++)
            {
                if (IsPlayerHand(overlapResults[i]))
                {
                    isCoveringCoverSpace = true;
                    break;
                }
            }

            if (isCoveringCoverSpace)
            {
                coverTime += Time.deltaTime;

                if (coverTime >= movementStopDelay && !playerHandStopped)
                {
                    playerHand.MovementEnabled = false;
                    playerHandStopped = true;
                    Debug.Log("PlayerHand movement stopped");
                }

                if (coverTime >= winDelay && gameSession != null)
                {
                    gameSession.TriggerWin();
                    coverTime = 0f;
                    isCoveringCoverSpace = false;
                    playerHandStopped = false;
                }
            }
            else
            {
                coverTime = 0f;
                playerHandStopped = false;
            }
        }

        if (!personalSpace.enabled) isInPersonalSpace = false;
        if (!recoilSpace.enabled) isInRecoilSpace = false;

        if (continuousDetection)
        {
            detectionTimer += Time.deltaTime;
            if (detectionTimer >= detectionInterval)
            {
                detectionTimer = 0f;
                CheckContinuousOverlaps();
                CheckMovementSpaceInteraction();
            }
        }

        HandleMovementSpaceBehavior();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    #region Movement Space Logic
    private void HandleMovementSpaceBehavior()
    {
        if (movementSpace != null && movementSpace.enabled)
        {
            // Update movement space visualization
            if (movementSpaceVisual != null)
            {
                movementSpaceVisual.size = new Vector2(movementSpace.radius * 2, movementSpace.radius * 2);
            }

            int hits = Physics2D.OverlapCollider(movementSpace, contactFilter, overlapResults);
            bool playerInMovementSpace = false;

            for (int i = 0; i < hits; i++)
            {
                if (IsPlayerHand(overlapResults[i]))
                {
                    playerInMovementSpace = true;
                    break;
                }
            }

            if (playerInMovementSpace)
            {
                Debug.Log("Player touched movement space");
                ResetMovementSpace();
                return;
            }

            if (!isMovingTowardPlayer)
            {
                isMovingTowardPlayer = true;
                movementSpaceOriginalPosition = transform.position;
                SetSortingLayer(coveringSortingLayer);
                Debug.Log("Started moving toward player");
            }

            // Smooth movement toward player
            float step = otherHandApproachSpeed * Time.deltaTime;
            Vector2 targetPos = new Vector2(
                playerHand.transform.position.x,
                transform.position.y
            );
            transform.position = Vector2.MoveTowards(transform.position, targetPos, step);
        }
        else if (isMovingTowardPlayer && !hasReachedPlayer)
        {
            // Continue moving until cover space makes contact
            float step = otherHandApproachSpeed * Time.deltaTime;
            Vector2 targetPos = new Vector2(
                playerHand.transform.position.x,
                transform.position.y
            );

            // Check if contact point is reached
            float distanceToPlayer = Vector2.Distance(transform.position, targetPos);
            if (distanceToPlayer <= coverSpaceContactOffset)
            {
                hasReachedPlayer = true;
                Debug.Log("Reached player contact point");
            }
            else
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPos, step);
            }
        }
    }

    private IEnumerator SmoothReturnToOriginalPosition()
    {
        if (movementSpace != null)
        {
            if (movementSpaceVisual != null)
            {
                Destroy(movementSpaceVisual.gameObject);
            }
            Destroy(movementSpace.gameObject);
            movementSpace = null;
            movementSpaceVisual = null;
        }

        isMovingTowardPlayer = false;
        hasReachedPlayer = false;
        movementShrinking = false;
        movementPending = false;

        float distance = Vector2.Distance(transform.position, originalPosition);
        float duration = distance / returnToOriginSpeed;
        float elapsedTime = 0f;
        Vector2 startPosition = transform.position;

        while (elapsedTime < duration)
        {
            transform.position = Vector2.Lerp(
                startPosition,
                originalPosition,
                elapsedTime / duration
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        SetSortingLayer(defaultSortingLayer);

        personalSpace.enabled = true;
        recoilSpace.enabled = true;
        personalSpace.radius = originalPersonalRadius;
        recoilSpace.radius = recoilSpaceRadius;
        shrinking = true;
    }

    private void SetSortingLayer(string layerName)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = layerName;
        }

        foreach (Transform child in transform)
        {
            var childRenderer = child.GetComponent<SpriteRenderer>();
            if (childRenderer != null)
            {
                childRenderer.sortingLayerName = layerName;
            }
        }
    }

    private void CheckMovementSpaceInteraction()
    {
        if (movementSpace != null && movementSpace.enabled)
        {
            int hits = Physics2D.OverlapCollider(movementSpace, contactFilter, overlapResults);
            for (int i = 0; i < hits; i++)
            {
                if (IsPlayerHand(overlapResults[i]))
                {
                    ResetMovementSpace();
                    break;
                }
            }
        }
    }

    private void ResetMovementSpace()
    {
        StartCoroutine(SmoothReturnToOriginalPosition());
    }
    #endregion

    #region Animation Logic
    private void HandleShrinkingAnimation()
    {
        if (shrinking && personalSpace != null)
        {
            if (isInPersonalSpace)
            {
                personalSpace.radius = originalPersonalRadius;
                recoilSpace.radius = recoilSpaceRadius;
                return;
            }

            personalSpace.radius = Mathf.MoveTowards(
                personalSpace.radius,
                coverSpaceRadius,
                shrinkSpeed * Time.deltaTime
            );

            if (personalSpace.radius <= recoilSpaceRadius && recoilSpace != null)
            {
                recoilSpace.radius = Mathf.MoveTowards(
                    recoilSpace.radius,
                    coverSpaceRadius,
                    shrinkSpeed * Time.deltaTime
                );
            }

            if (personalSpace.radius <= coverSpaceRadius &&
                (recoilSpace == null || recoilSpace.radius <= coverSpaceRadius))
            {
                personalSpace.enabled = false;
                if (recoilSpace != null) recoilSpace.enabled = false;

                isInPersonalSpace = false;
                isInRecoilSpace = false;

                shrinking = false;
                movementPending = true;
                movementDelayTimer = 0f;
            }
        }

        if (movementPending)
        {
            movementDelayTimer += Time.deltaTime;
            if (movementDelayTimer >= movementDelay)
            {
                CreateProximityCollider(ref movementSpace, originalPersonalRadius, "MovementSpace");
                originalMovementRadius = originalPersonalRadius;
                movementShrinking = true;
                movementPending = false;
                movementSpaceOriginalPosition = transform.position;
                Debug.Log("Movement space created");
            }
        }

        if (movementShrinking && movementSpace != null)
        {
            movementSpace.radius = Mathf.MoveTowards(
                movementSpace.radius,
                coverSpaceRadius,
                movementSpaceShrinkSpeed * Time.deltaTime
            );

            if (movementSpaceVisual != null)
            {
                movementSpaceVisual.size = new Vector2(movementSpace.radius * 2, movementSpace.radius * 2);
            }

            if (movementSpace.radius <= coverSpaceRadius)
            {
                movementSpace.enabled = false;
                movementShrinking = false;
                Debug.Log("Movement space completed");
            }
        }
    }
    #endregion

    #region Collider Management
    private void CreateProximityCollider(ref CircleCollider2D col, float radius, string name, float verticalOffset = 0f)
    {
        var sensor = new GameObject(name)
        {
            transform =
            {
                parent = transform,
                localPosition = new Vector3(0, verticalOffset, 0)
            }
        };

        col = sensor.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;

        // Visualization for movement space
        if (name == "MovementSpace")
        {
            movementSpaceVisual = sensor.AddComponent<SpriteRenderer>();
            movementSpaceVisual.sprite = Sprite.Create(
                new Texture2D(1, 1),
                new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f)
            );
            movementSpaceVisual.color = movementSpaceColor;
            movementSpaceVisual.drawMode = SpriteDrawMode.Sliced;
            movementSpaceVisual.size = new Vector2(radius * 2, radius * 2);
            movementSpaceVisual.sortingLayerName = "Debug";
        }

        var rb = sensor.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void CreateBoxProximityCollider(ref BoxCollider2D col, float size, string name, float verticalOffset = 0f)
    {
        var sensor = new GameObject(name)
        {
            transform =
            {
                parent = transform,
                localPosition = new Vector3(0, verticalOffset, 0)
            }
        };

        col = sensor.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(size, size);

        var rb = sensor.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void CalculatePlayerColliderBounds()
    {
        if (playerHandCollider != null)
        {
            Vector2 colliderCenter = playerHand.transform.TransformPoint(playerHandCollider.offset);
            float worldRadius = playerHandCollider.radius * Mathf.Max(
                playerHand.transform.lossyScale.x,
                playerHand.transform.lossyScale.y
            );
            playerLeftBound = colliderCenter.x - worldRadius;
        }
    }
    #endregion

    #region Collision Detection
    private void CheckContinuousOverlaps()
    {
        isInPersonalSpace = false;
        isInRecoilSpace = false;

        foreach (var collider in GetActiveColliders())
        {
            if (collider == null || collider == coverSpace) continue;

            int hits = Physics2D.OverlapCollider(collider, contactFilter, overlapResults);

            for (int i = 0; i < hits; i++)
            {
                if (IsPlayerHand(overlapResults[i]))
                {
                    if (collider == personalSpace) isInPersonalSpace = true;
                    if (collider == recoilSpace && !isLossTriggered)
                    {
                        isInRecoilSpace = true;
                        PlayRandomRecoilSound();
                        TriggerLossSequence();
                    }
                    break;
                }
            }
        }
    }

    private CircleCollider2D[] GetActiveColliders()
    {
        return new[]
        {
            personalSpace?.enabled == true ? personalSpace : null,
            recoilSpace?.enabled == true ? recoilSpace : null,
            movementSpace?.enabled == true ? movementSpace : null
        };
    }
    #endregion

    #region Movement Logic
    private void HandleMovement()
    {
        if (isMovingTowardPlayer) return;

        float otherHandLeftEdge = transform.position.x - (otherHandWidth * 0.5f * transform.localScale.x);
        float otherHandRightEdge = transform.position.x + (otherHandWidth * 0.5f * transform.localScale.x);

        bool shouldMove = (personalSpace.enabled && isInPersonalSpace) ||
                         (recoilSpace.enabled && isInRecoilSpace);

        if (shouldMove && playerHand != null)
        {
            float currentPushSpeed = pushSpeed;
            if (isInRecoilSpace) currentPushSpeed *= recoilPushMultiplier;

            float distanceX = Mathf.Abs(playerLeftBound - otherHandRightEdge);
            float activeRadius = isInRecoilSpace ? recoilSpace.radius : personalSpace.radius;

            if (distanceX < activeRadius)
            {
                float pushDirection = Mathf.Sign(transform.position.x - playerHand.transform.position.x);
                float pushDistance = activeRadius - distanceX;

                float desiredX = transform.position.x + pushDirection * pushDistance * currentPushSpeed * Time.fixedDeltaTime;
                float newRightEdge = desiredX + (otherHandWidth * 0.5f * transform.localScale.x);

                if (newRightEdge > playerLeftBound)
                {
                    desiredX = playerLeftBound - (otherHandWidth * 0.5f * transform.localScale.x);
                }

                targetPosition.x = desiredX;

                if (playerHandRb != null)
                {
                    float velocityAdjustedX = targetPosition.x + playerHandRb.linearVelocity.x * Time.fixedDeltaTime;
                    float adjustedRightEdge = velocityAdjustedX + (otherHandWidth * 0.5f * transform.localScale.x);

                    if (adjustedRightEdge <= playerLeftBound)
                    {
                        targetPosition.x = velocityAdjustedX;
                    }
                }
            }
        }
        else
        {
            targetPosition.x = Mathf.MoveTowards(
                transform.position.x,
                originalPosition.x,
                returnSpeed * Time.fixedDeltaTime
            );
        }

        Vector2 newPosition = Vector2.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            movementSmoothing
        );

        transform.position = newPosition;
    }
    #endregion

    #region Helper Methods
    private bool IsPlayerHand(Collider2D other)
    {
        if (playerHand != null && other.gameObject == playerHand) return true;
        return playerHandLayer.value != 0 &&
               (playerHandLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    private void PlayRandomRecoilSound()
    {
        if (recoilSounds != null && recoilSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = Random.Range(0, recoilSounds.Length);
            audioSource.PlayOneShot(recoilSounds[randomIndex]);
        }
    }

    private void TriggerLossSequence()
    {
        if (isLossTriggered || gameSession == null) return;

        isLossTriggered = true;

        shrinking = false;
        movementPending = false;
        movementShrinking = false;

        personalSpace?.gameObject.SetActive(false);
        recoilSpace?.gameObject.SetActive(false);
        coverSpace?.gameObject.SetActive(false);
        movementSpace?.gameObject.SetActive(false);

        StartCoroutine(PlayLossAnimationAndTriggerLoss());
    }

    private IEnumerator PlayLossAnimationAndTriggerLoss()
    {
        yield return new WaitForSeconds(lossAnimationDuration);
        Debug.Log("Triggering loss condition now!");
        gameSession.TriggerLoss();
    }
    #endregion

    #region Debug Tools
    private void OnDrawGizmos()
    {
        // Always draw in editor if drawCollidersInEditor is true
        bool shouldDraw = Application.isPlaying ? visualizeColliders : drawCollidersInEditor;
        if (!shouldDraw) return;

        Vector2 currentPos = transform.position;
        float playerLeftEdge = currentPos.x + 2f;
        if (playerHand != null && playerHandCollider != null)
        {
            Vector2 colliderCenter = playerHand.transform.TransformPoint(playerHandCollider.offset);
            float worldRadius = playerHandCollider.radius * Mathf.Max(
                playerHand.transform.lossyScale.x,
                playerHand.transform.lossyScale.y
            );
            playerLeftEdge = colliderCenter.x - worldRadius;
        }

        // OtherHand's bounds
        float otherHandLeft = currentPos.x - (otherHandWidth * 0.5f * transform.localScale.x);
        float otherHandRight = currentPos.x + (otherHandWidth * 0.5f * transform.localScale.x);

        Gizmos.color = new Color(0, 0, 1, 0.6f);
        Gizmos.DrawLine(new Vector3(otherHandLeft, currentPos.y - 1, 0),
                       new Vector3(otherHandLeft, currentPos.y + 1, 0));
        Gizmos.DrawLine(new Vector3(otherHandRight, currentPos.y - 1, 0),
                       new Vector3(otherHandRight, currentPos.y + 1, 0));

        // PlayerHand's left edge
        if (playerHand != null && playerHandCollider != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.8f);
            Gizmos.DrawLine(new Vector3(playerLeftEdge, currentPos.y - 2, 0),
                           new Vector3(playerLeftEdge, currentPos.y + 2, 0));
        }

        // Approximations in editor
        if (Application.isPlaying)
        {
            // Runtime drawing
            if (coverSpace != null)
            {
                Gizmos.color = new Color(0, 1, 0, coverSpace.enabled ? 0.5f : 0.2f);
                Gizmos.DrawWireCube(coverSpace.transform.position, coverSpace.size);
            }

            if (personalSpace != null)
            {
                Gizmos.color = new Color(1, 1, 0, personalSpace.enabled ? 0.5f : 0.2f);
                Gizmos.DrawWireSphere(personalSpace.transform.position, personalSpace.radius);
            }

            if (recoilSpace != null)
            {
                Gizmos.color = new Color(1, 0, 0, recoilSpace.enabled ? 0.5f : 0.2f);
                Gizmos.DrawWireSphere(recoilSpace.transform.position, recoilSpace.radius);
            }

            if (movementSpace != null)
            {
                Gizmos.color = new Color(1, 0, 1, movementSpace.enabled ? 0.5f : 0.2f);
                Gizmos.DrawWireSphere(movementSpace.transform.position, movementSpace.radius);
            }
        }
        else
        {
            // Approximations based on serialized values
            Vector2 editorOffset = Vector2.zero;

            // Cover Space
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireCube(
                transform.position + new Vector3(0, coverSpaceVerticalOffset, 0),
                new Vector3(coverSpaceRadius, coverSpaceRadius, 0)
            );

            // Personal Space
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(
                transform.position + new Vector3(0, 0, 0),
                personalSpaceRadius
            );

            // Recoil Space
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawWireSphere(
                transform.position + new Vector3(0, recoilSpaceVerticalOffset, 0),
                recoilSpaceRadius
            );

            // Movement Space is the same as personal space in editor
            Gizmos.color = new Color(1, 0, 1, 0.5f);
            Gizmos.DrawWireSphere(
                transform.position + new Vector3(0, 0, 0),
                personalSpaceRadius
            );
        }

        if (originalPosition != Vector2.zero)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(originalPosition, maxDistanceFromOrigin);
        }
    }
    #endregion
}
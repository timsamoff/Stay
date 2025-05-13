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

    /* [Header("Loss Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string lossAnimationTrigger = "Lose"; */

    [Header("Recoil Sounds")]
    [SerializeField] private AudioClip[] recoilSounds;
    [SerializeField] private AudioSource audioSource;

    [Header("Debug")]
    [SerializeField] private bool visualizeColliders = true;
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

    private void Awake()
    {
        contactFilter = new ContactFilter2D().NoFilter();
        originalPersonalRadius = personalSpaceRadius;
        originalPosition = transform.position;
        targetPosition = originalPosition;

        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

            // Time while covering
            if (isCoveringCoverSpace)
            {
                coverTime += Time.deltaTime;

                // Stop movement first
                if (coverTime >= movementStopDelay && !playerHandStopped)
                {
                    playerHand.MovementEnabled = false;
                    playerHandStopped = true;
                    Debug.Log("PlayerHand movement stopped");
                }

                // Then trigger win after longer delay
                if (coverTime >= winDelay && gameSession != null)
                {
                    gameSession.TriggerWin();
                    coverTime = 0f;
                    isCoveringCoverSpace = false;
                    playerHandStopped = false; // Reset for next time
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
            }
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    #region Animation Logic
    private void HandleShrinkingAnimation()
    {
        if (shrinking && personalSpace != null)
        {
            if (isInPersonalSpace)
            {
                // Reset both spaces to original size when touched
                personalSpace.radius = originalPersonalRadius;
                recoilSpace.radius = recoilSpaceRadius;
                return;
            }

            // Shrink personal space using the randomized shrinkSpeed
            personalSpace.radius = Mathf.MoveTowards(
                personalSpace.radius,
                coverSpaceRadius,
                shrinkSpeed * Time.deltaTime
            );

            // When personal space reaches recoil space size, shrink recoil space too
            if (personalSpace.radius <= recoilSpaceRadius && recoilSpace != null)
            {
                recoilSpace.radius = Mathf.MoveTowards(
                    recoilSpace.radius,
                    coverSpaceRadius,
                    shrinkSpeed * Time.deltaTime
                );
            }

            // Disable when both reach cover space size
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
                movementShrinking = true;
                movementPending = false;
            }
        }

        if (movementShrinking && movementSpace != null)
        {
            movementSpace.radius = Mathf.MoveTowards(
                movementSpace.radius,
                coverSpaceRadius,
                shrinkSpeed * Time.deltaTime
            );

            if (movementSpace.radius <= coverSpaceRadius)
            {
                movementSpace.enabled = false;
                movementShrinking = false;
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
                        PlayRandomRecoilSound(); // New sound playback
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

        // Disable game mechanics
        shrinking = false;
        movementPending = false;
        movementShrinking = false;

        // Disable all colliders
        personalSpace?.gameObject.SetActive(false);
        recoilSpace?.gameObject.SetActive(false);
        coverSpace?.gameObject.SetActive(false);
        movementSpace?.gameObject.SetActive(false);

        // Start the loss sequence
        StartCoroutine(PlayLossAnimationAndTriggerLoss());
    }

    private IEnumerator PlayLossAnimationAndTriggerLoss()
    {
        /* if (animator != null)
        {
            animator.SetTrigger(lossAnimationTrigger);

            // Wait for animation to complete
            yield return new WaitForSeconds(lossAnimationDuration);

            // Alternative: Wait for animation state to complete
            // yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
        }
        else
        {
            // No animator, just wait the duration
            yield return new WaitForSeconds(lossAnimationDuration);
        } */

        yield return new WaitForSeconds(lossAnimationDuration); // Remove this once the animation is made

        Debug.Log("Triggering loss condition now!");
        gameSession.TriggerLoss();
    }
    #endregion

    #region Debug Tools
    private void OnDrawGizmos()
    {
        if (!visualizeColliders) return;

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

        // Draw colliders
        if (coverSpace != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            Gizmos.DrawWireCube(coverSpace.transform.position, coverSpace.size);
        }

        if (personalSpace != null && personalSpace.enabled)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(personalSpace.transform.position, personalSpace.radius);
        }

        if (recoilSpace != null && recoilSpace.enabled)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawWireSphere(recoilSpace.transform.position, recoilSpace.radius);
        }

        if (originalPosition != Vector2.zero)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(originalPosition, maxDistanceFromOrigin);
        }
    }
    #endregion
}
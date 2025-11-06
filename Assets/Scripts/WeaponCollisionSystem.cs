using UnityEngine;

/// <summary>
/// Prevents weapon from clipping through walls by detecting collisions
/// and smoothly pushing the weapon back toward the player.
/// Attach to Player GameObject alongside CharacterAnimatorController
/// </summary>
public class WeaponCollisionSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform weaponPivot;

    [Header("Collision Settings")]
    [Tooltip("Enable/disable the entire collision system")]
    [SerializeField] private bool enableCollision = true;

    [Tooltip("Maximum distance weapon can be pushed back (in units/meters)")]
    [SerializeField] private float maxPushbackDistance = 0.3f;

    [Tooltip("How smoothly weapon moves back (higher = smoother)")]
    [SerializeField] private float collisionSmoothness = 10f;

    [Tooltip("How much weapon rotates when near wall (degrees)")]
    [SerializeField] private float rotationAmount = 15f;

    [Tooltip("Detection range in front of weapon")]
    [SerializeField] private float detectionDistance = 0.5f;

    [Tooltip("Layers that weapon collides with (set to Environment for best performance)")]
    [SerializeField] private LayerMask collisionLayers = ~0;

    [Header("Advanced Settings")]
    [Tooltip("Minimum pushback threshold (prevents micro-adjustments)")]
    [SerializeField] private float minimumPushback = 0.01f;

    [Tooltip("Enable weapon scaling when very close to wall")]
    [SerializeField] private bool enableScaling = false;

    [Tooltip("How much to scale weapon when fully pushed back")]
    [SerializeField] private float minScale = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRays = false;
    [SerializeField] private Color debugRayColor = Color.yellow;

    // Internal state
    private float currentPushback = 0f;
    private float currentRotation = 0f;
    private float currentScale = 1f;
    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;
    private Vector3 originalWeaponScale;
    private bool isInitialized = false;

    void Start()
    {
        // Auto-find references if not assigned
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        if (playerCamera == null)
        {
            Debug.LogError("WeaponCollisionSystem: No camera found! Collision system disabled.");
            enableCollision = false;
        }
    }

    void LateUpdate()
    {
        // LateUpdate ensures this runs after CharacterAnimatorController
        if (!enableCollision || weaponPivot == null || playerCamera == null)
        {
            return;
        }

        // Store original transform on first frame
        if (!isInitialized && weaponPivot != null)
        {
            StoreOriginalTransform();
        }

        // Detect collisions and calculate adjustments
        DetectCollision();

        // Apply collision offset to weapon
        ApplyCollisionOffset();
    }

    void StoreOriginalTransform()
    {
        originalWeaponPosition = weaponPivot.localPosition;
        originalWeaponRotation = weaponPivot.localRotation;
        originalWeaponScale = weaponPivot.localScale;
        isInitialized = true;
    }

    void DetectCollision()
    {
        // Get positions
        Vector3 cameraPos = playerCamera.transform.position;
        Vector3 weaponWorldPos = weaponPivot.position;

        // Calculate direction and distance
        Vector3 directionToWeapon = weaponWorldPos - cameraPos;
        float distanceToWeapon = directionToWeapon.magnitude;

        // Raycast from camera toward weapon (and slightly beyond)
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(
            cameraPos,
            directionToWeapon.normalized,
            out hit,
            distanceToWeapon + detectionDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore // Ignore trigger colliders
        );

        // Debug visualization
        if (showDebugRays)
        {
            if (hitSomething)
            {
                Debug.DrawLine(cameraPos, hit.point, Color.red);
                Debug.DrawLine(hit.point, weaponWorldPos, Color.yellow);
            }
            else
            {
                Debug.DrawLine(cameraPos, weaponWorldPos, Color.green);
            }
        }

        // Calculate target pushback
        float targetPushback = 0f;
        float targetRotation = 0f;
        float targetScale = 1f;

        if (hitSomething)
        {
            float hitDistance = hit.distance;

            // Check if weapon would clip through wall
            if (hitDistance < distanceToWeapon)
            {
                // Calculate penetration depth
                float penetrationDepth = distanceToWeapon - hitDistance;
                targetPushback = Mathf.Clamp(penetrationDepth, 0f, maxPushbackDistance);

                // Only apply if above minimum threshold
                if (targetPushback < minimumPushback)
                {
                    targetPushback = 0f;
                }

                // Calculate rotation based on wall normal
                Vector3 wallNormal = hit.normal;
                float wallAngle = Vector3.Angle(playerCamera.transform.forward, -wallNormal);
                targetRotation = Mathf.Clamp(wallAngle * 0.3f, 0f, rotationAmount);

                // Calculate scale if enabled
                if (enableScaling && targetPushback > 0.1f)
                {
                    float scaleProgress = targetPushback / maxPushbackDistance;
                    targetScale = Mathf.Lerp(1f, minScale, scaleProgress);
                }
            }
        }

        // Smooth interpolation to target values
        currentPushback = Mathf.Lerp(currentPushback, targetPushback, Time.deltaTime * collisionSmoothness);
        currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * collisionSmoothness);
        currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * collisionSmoothness);
    }

    void ApplyCollisionOffset()
    {
        if (!isInitialized) return;

        // Get current position/rotation (includes sway, recoil, etc.)
        Vector3 currentPos = weaponPivot.localPosition;
        Quaternion currentRot = weaponPivot.localRotation;
        Vector3 currentScaleVec = weaponPivot.localScale;

        // Apply pushback (negative Z = back toward player)
        Vector3 collisionOffset = new Vector3(0, 0, -currentPushback);
        weaponPivot.localPosition = currentPos + collisionOffset;

        // Apply rotation (Y-axis rotation for wall angle)
        Quaternion collisionRotation = Quaternion.Euler(0, currentRotation, 0);
        weaponPivot.localRotation = currentRot * collisionRotation;

        // Apply scale if enabled
        if (enableScaling)
        {
            weaponPivot.localScale = currentScaleVec * currentScale;
        }
    }

    // ========================
    // PUBLIC METHODS
    // ========================

    /// <summary>
    /// Call this when equipping a new weapon
    /// </summary>
    public void SetWeaponPivot(Transform pivot)
    {
        weaponPivot = pivot;
        isInitialized = false;

        // Reset state
        currentPushback = 0f;
        currentRotation = 0f;
        currentScale = 1f;
    }

    /// <summary>
    /// Enable or disable collision detection
    /// </summary>
    public void SetCollisionEnabled(bool enabled)
    {
        enableCollision = enabled;

        // Reset when disabling
        if (!enabled)
        {
            currentPushback = 0f;
            currentRotation = 0f;
            currentScale = 1f;
        }
    }

    /// <summary>
    /// Get current pushback distance (useful for debugging)
    /// </summary>
    public float GetCurrentPushback()
    {
        return currentPushback;
    }

    /// <summary>
    /// Check if weapon is currently colliding
    /// </summary>
    public bool IsColliding()
    {
        return currentPushback > minimumPushback;
    }

    /// <summary>
    /// Override collision settings for specific weapon types
    /// </summary>
    public void SetCollisionSettings(float maxPushback, float smoothness, float rotation)
    {
        maxPushbackDistance = maxPushback;
        collisionSmoothness = smoothness;
        rotationAmount = rotation;
    }

    /// <summary>
    /// Reset collision state (useful when switching weapons)
    /// </summary>
    public void ResetCollision()
    {
        currentPushback = 0f;
        currentRotation = 0f;
        currentScale = 1f;
    }

    // ========================
    // GIZMOS (Editor Only)
    // ========================

    void OnDrawGizmosSelected()
    {
        if (playerCamera == null || weaponPivot == null) return;

        // Draw detection sphere at weapon position
        Gizmos.color = IsColliding() ? Color.red : Color.green;
        Gizmos.DrawWireSphere(weaponPivot.position, 0.1f);

        // Draw detection range
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 cameraPos = playerCamera.transform.position;
        Vector3 weaponPos = weaponPivot.position;
        Vector3 direction = (weaponPos - cameraPos).normalized;
        Gizmos.DrawLine(cameraPos, weaponPos + direction * detectionDistance);
    }
}
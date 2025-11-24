using UnityEngine;

/// <summary>
/// Camera recoil that pushes player's view upward, forcing them to pull down to compensate
/// Attach to the player camera or FPSCharacterController
/// </summary>
public class CameraRecoil : MonoBehaviour
{
    [Header("=== RECOIL SETTINGS ===")]
    [Tooltip("How much camera rotates up per shot (degrees)")]
    [SerializeField] private float verticalRecoil = 0.5f;

    [Tooltip("Random horizontal push (degrees)")]
    [SerializeField] private float horizontalRecoil = 0.3f;

    [Tooltip("Random variance for horizontal")]
    [SerializeField] private float horizontalVariance = 0.2f;

    [Header("=== RECOVERY ===")]
    [Tooltip("How fast recoil is applied (snappy)")]
    [SerializeField] private float recoilSpeed = 15f;

    [Tooltip("How fast camera recovers (should be SLOWER than recoil speed)")]
    [SerializeField] private float recoverySpeed = 2f;

    [Tooltip("Recovery starts after this delay (seconds)")]
    [SerializeField] private float recoveryDelay = 0.1f;

    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private Camera playerCamera;

    // Recoil state
    private Vector2 currentRecoil = Vector2.zero; // x = horizontal, y = vertical
    private Vector2 targetRecoil = Vector2.zero;
    private float lastRecoilTime = 0f;

    // Player look rotation
    private float currentPitch = 0f; // Vertical rotation
    private float currentYaw = 0f;   // Horizontal rotation

    void Start()
    {
        // Auto-find references
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();

        if (playerBody == null)
            playerBody = transform.parent;

        if (playerCamera == null || playerBody == null)
        {
            Debug.LogError("[CameraRecoil] Missing references! Attach to player camera.");
            enabled = false;
        }
    }

    void LateUpdate()
    {
        // Check if we should start recovering
        bool shouldRecover = Time.time - lastRecoilTime > recoveryDelay;

        // Apply recoil quickly
        currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilSpeed);

        // Recover slowly
        if (shouldRecover)
        {
            targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, Time.deltaTime * recoverySpeed);
        }

        // Apply recoil to camera rotation
        ApplyRecoilToCamera();
    }

    void ApplyRecoilToCamera()
    {
        // Add recoil to current rotation
        currentPitch -= currentRecoil.y; // Negative because Unity's pitch is inverted
        currentYaw += currentRecoil.x;

        // Apply to camera (pitch only)
        playerCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

        // Apply to player body (yaw only)
        playerBody.localRotation = Quaternion.Euler(0f, currentYaw, 0f);

        // Reset recoil offset after applying (so it doesn't accumulate)
        currentRecoil = Vector2.zero;
    }

    /// <summary>
    /// Apply camera recoil - call this when weapon fires
    /// </summary>
    public void ApplyRecoil(float multiplier = 1f)
    {
        // Add vertical recoil (push camera up)
        targetRecoil.y += verticalRecoil * multiplier;

        // Add horizontal recoil with randomness (push camera left/right)
        float horizontalKick = Random.Range(-horizontalVariance, horizontalVariance);
        targetRecoil.x += (horizontalRecoil + horizontalKick) * multiplier;

        lastRecoilTime = Time.time;
    }

    /// <summary>
    /// Apply camera recoil with pattern
    /// </summary>
    public void ApplyRecoil(Vector2 recoilPattern)
    {
        targetRecoil.y += recoilPattern.x; // Vertical

        float horizontalKick = Random.Range(-horizontalVariance, horizontalVariance);
        targetRecoil.x += recoilPattern.y + horizontalKick; // Horizontal

        lastRecoilTime = Time.time;
    }

    /// <summary>
    /// Apply camera recoil with custom weapon values (per-weapon recoil)
    /// </summary>
    public void ApplyRecoil(float weaponVertical, float weaponHorizontal, float weaponVariance)
    {
        // Use weapon-specific values instead of default
        targetRecoil.y += weaponVertical;

        float horizontalKick = Random.Range(-weaponVariance, weaponVariance);
        targetRecoil.x += weaponHorizontal + horizontalKick;

        lastRecoilTime = Time.time;
    }

    /// <summary>
    /// Apply camera recoil with custom weapon values AND resistance (per-weapon recoil with resistance)
    /// </summary>
    public void ApplyRecoil(float weaponVertical, float weaponHorizontal, float weaponVariance, float customRecoverySpeed)
    {
        // Use weapon-specific values
        targetRecoil.y += weaponVertical;

        float horizontalKick = Random.Range(-weaponVariance, weaponVariance);
        targetRecoil.x += weaponHorizontal + horizontalKick;

        // Override recovery speed for this weapon
        recoverySpeed = customRecoverySpeed;

        lastRecoilTime = Time.time;
    }

    /// <summary>
    /// Update current rotation (call this from player controller)
    /// </summary>
    public void UpdateRotation(float pitch, float yaw)
    {
        currentPitch = pitch;
        currentYaw = yaw;
    }

    /// <summary>
    /// Get current pitch with recoil applied
    /// </summary>
    public float GetPitch() => currentPitch;

    /// <summary>
    /// Get current yaw with recoil applied
    /// </summary>
    public float GetYaw() => currentYaw;
}
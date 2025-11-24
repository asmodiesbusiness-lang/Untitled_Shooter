using UnityEngine;

public class DualPointRecoil : MonoBehaviour
{
    [Header("=== RECOIL SETTINGS ===")]
    [Tooltip("How much gun pitches UP per shot (degrees)")]
    [SerializeField] private float verticalRecoil = 2.5f;

    [Tooltip("How much gun rolls LEFT/RIGHT per shot (degrees)")]
    [SerializeField] private float horizontalRecoil = 0.8f;

    [Tooltip("Random variance for horizontal roll")]
    [SerializeField] private float horizontalVariance = 0.5f;

    [Header("=== KICKBACK (Position) ===")]
    [Tooltip("How far gun kicks back (Z axis)")]
    [SerializeField] private float kickbackDistance = 0.05f;

    [Tooltip("How fast gun snaps back (higher = more snappy)")]
    [SerializeField] private float kickbackSpeed = 30f;

    [Tooltip("How fast gun returns forward (higher = more snappy)")]
    [SerializeField] private float kickbackReturnSpeed = 20f;

    [Header("=== RECOVERY ===")]
    [SerializeField] private float recoilSpeed = 10f;
    [SerializeField] private float recoverySpeed = 5f;

    [Header("=== MUZZLE POINT ===")]
    [Tooltip("Where bullets come from - auto-found if empty")]
    public Transform muzzlePoint;

    // Rotation state
    private Vector3 currentRotation = Vector3.zero;
    private Vector3 targetRotation = Vector3.zero;

    // Kickback state
    private Vector3 originalPosition;
    private Vector3 currentPosition;
    private Vector3 targetPosition;
    private bool isKickingBack = false;

    void Start()
    {
        // Auto-find muzzle point
        if (muzzlePoint == null)
        {
            muzzlePoint = transform.Find("MuzzlePoint");
            if (muzzlePoint == null)
                Debug.LogWarning("[DualPointRecoil] No MuzzlePoint found!");
        }

        // Store original position
        originalPosition = transform.localPosition;
        currentPosition = originalPosition;
        targetPosition = originalPosition;
    }

    void Update()
    {
        // Rotation recoil (smooth)
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * recoilSpeed);
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, Time.deltaTime * recoverySpeed);
        transform.localRotation = Quaternion.Euler(currentRotation);

        // Kickback position (SNAPPY)
        if (isKickingBack)
        {
            // Snap back FAST
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * kickbackSpeed);

            // Check if we've reached the back position
            if (Vector3.Distance(currentPosition, targetPosition) < 0.001f)
            {
                isKickingBack = false;
                targetPosition = originalPosition; // Start returning
            }
        }
        else
        {
            // Snap forward FAST
            currentPosition = Vector3.Lerp(currentPosition, originalPosition, Time.deltaTime * kickbackReturnSpeed);
        }

        transform.localPosition = currentPosition;
    }

    /// <summary>
    /// Apply recoil - gun pitches up, rolls randomly, and kicks back
    /// </summary>
    public void ApplyRecoil(float multiplier = 1f)
    {
        // PITCH UP (X rotation = looking up)
        targetRotation.x -= verticalRecoil * multiplier;

        // ROLL left/right (Z rotation = tilt)
        float randomRoll = Random.Range(-horizontalVariance, horizontalVariance);
        targetRotation.z += (horizontalRecoil + randomRoll) * multiplier;

        // KICKBACK (snap back on Z axis)
        targetPosition = originalPosition + new Vector3(0, 0, -kickbackDistance * multiplier);
        isKickingBack = true;
    }

    /// <summary>
    /// Apply recoil with pattern (x = vertical, y = horizontal)
    /// </summary>
    public void ApplyRecoil(Vector2 recoilPattern)
    {
        // Vertical = pitch up
        targetRotation.x -= recoilPattern.x;

        // Horizontal = roll with randomness
        float randomRoll = Random.Range(-horizontalVariance, horizontalVariance);
        targetRotation.z += (recoilPattern.y + randomRoll);

        // Kickback
        targetPosition = originalPosition + new Vector3(0, 0, -kickbackDistance);
        isKickingBack = true;
    }

    /// <summary>
    /// Get the direction bullets should fire (accounts for recoil)
    /// </summary>
    public Vector3 GetMuzzleDirection()
    {
        if (muzzlePoint != null)
            return muzzlePoint.forward;
        return transform.forward;
    }

    /// <summary>
    /// Get muzzle position for bullet spawn
    /// </summary>
    public Vector3 GetMuzzlePosition()
    {
        if (muzzlePoint != null)
            return muzzlePoint.position;
        return transform.position;
    }

    /// <summary>
    /// Reset recoil instantly
    /// </summary>
    public void ResetRecoil()
    {
        currentRotation = Vector3.zero;
        targetRotation = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        currentPosition = originalPosition;
        targetPosition = originalPosition;
        transform.localPosition = originalPosition;
        isKickingBack = false;
    }

    /// <summary>
    /// Check if gun is currently recoiling
    /// </summary>
    public bool IsRecoiling()
    {
        return currentRotation.magnitude > 0.1f || isKickingBack;
    }

    void OnDrawGizmosSelected()
    {
        if (muzzlePoint != null)
        {
            // Draw muzzle point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(muzzlePoint.position, 0.02f);

            // Draw firing direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * 0.5f);
        }
    }
}
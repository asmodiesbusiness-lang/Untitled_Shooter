using UnityEngine;

public class DualPointRecoil : MonoBehaviour
{
    [Header("=== RECOIL SETTINGS ===")]
    [Tooltip("How much gun pitches UP per shot (degrees)")]
    [SerializeField] private float verticalRecoil = 2.5f;

    [Tooltip("Random horizontal YAW per shot (degrees)")]
    [SerializeField] private float horizontalRecoil = 0.8f;

    [Tooltip("Random variance for horizontal")]
    [SerializeField] private float horizontalVariance = 0.5f;

    [Header("=== KICKBACK (Position) ===")]
    [SerializeField] private float kickbackDistance = 0.05f;
    [SerializeField] private float kickbackSpeed = 30f;
    [SerializeField] private float kickbackReturnSpeed = 20f;

    [Header("=== RECOVERY ===")]
    [SerializeField] private float recoilSpeed = 10f;
    [SerializeField] private float recoverySpeed = 5f;

    [Header("=== MUZZLE POINT ===")]
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
        if (muzzlePoint == null)
        {
            muzzlePoint = transform.Find("MuzzlePoint");
            if (muzzlePoint == null)
                Debug.LogWarning("[DualPointRecoil] No MuzzlePoint found!");
        }

        originalPosition = transform.localPosition;
        currentPosition = originalPosition;
        targetPosition = originalPosition;
    }

    void Update()
    {
        // Rotation recoil
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * recoilSpeed);
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, Time.deltaTime * recoverySpeed);
        transform.localRotation = Quaternion.Euler(currentRotation);

        // Kickback position
        if (isKickingBack)
        {
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * kickbackSpeed);

            if (Vector3.Distance(currentPosition, targetPosition) < 0.001f)
            {
                isKickingBack = false;
                targetPosition = originalPosition;
            }
        }
        else
        {
            currentPosition = Vector3.Lerp(currentPosition, originalPosition, Time.deltaTime * kickbackReturnSpeed);
        }

        transform.localPosition = currentPosition;
    }

    public void ApplyRecoil(float multiplier = 1f)
    {
        // PITCH UP (negative X = look up in Unity)
        targetRotation.x -= verticalRecoil * multiplier;

        // YAW left/right (Y rotation)
        float randomYaw = Random.Range(-horizontalVariance, horizontalVariance);
        targetRotation.y += (horizontalRecoil + randomYaw) * multiplier * (Random.value > 0.5f ? 1f : -1f);

        // KICKBACK
        targetPosition = originalPosition + new Vector3(0, 0, -kickbackDistance * multiplier);
        isKickingBack = true;
    }

    public void ApplyRecoil(Vector2 recoilPattern)
    {
        // X = vertical (pitch up)
        targetRotation.x -= recoilPattern.x;

        // Y = horizontal (yaw)
        float randomYaw = Random.Range(-horizontalVariance, horizontalVariance);
        targetRotation.y += (recoilPattern.y + randomYaw) * (Random.value > 0.5f ? 1f : -1f);

        // Kickback
        targetPosition = originalPosition + new Vector3(0, 0, -kickbackDistance);
        isKickingBack = true;
    }

    public Vector3 GetMuzzleDirection()
    {
        return muzzlePoint != null ? muzzlePoint.forward : transform.forward;
    }

    public Vector3 GetMuzzlePosition()
    {
        return muzzlePoint != null ? muzzlePoint.position : transform.position;
    }

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

    public bool IsRecoiling() => currentRotation.magnitude > 0.1f || isKickingBack;

    void OnDrawGizmosSelected()
    {
        if (muzzlePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(muzzlePoint.position, 0.02f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * 0.5f);
        }
    }
}
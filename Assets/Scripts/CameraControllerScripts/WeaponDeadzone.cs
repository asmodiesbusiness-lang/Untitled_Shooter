using UnityEngine;

[DefaultExecutionOrder(200)]
public class WeaponDeadzone : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform weaponHolder;

    [Header("=== DEADZONE SETTINGS ===")]
    [SerializeField] private bool enableDeadzone = true;
    [SerializeField] private float maxDeadzoneAngle = 5f;
    [SerializeField] private float deadzoneSmoothing = 10f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugLogs = true;

    private Vector3 targetDeadzoneRotation;
    private Vector3 currentDeadzoneRotation;
    private Transform currentWeapon;
    private bool initialized = false;

    void Start()
    {
        // Try to find WeaponHolder if not assigned
        if (weaponHolder == null)
        {
            // First check children
            weaponHolder = transform.Find("WeaponHolder");

            // If not found, search scene
            if (weaponHolder == null)
            {
                GameObject holderObj = GameObject.Find("WeaponHolder");
                if (holderObj != null)
                    weaponHolder = holderObj.transform;
            }
        }

        initialized = weaponHolder != null;

        if (showDebugLogs)
        {
            if (initialized)
                Debug.Log($"[WeaponDeadzone] Initialized! Found WeaponHolder: {weaponHolder.name}");
            else
                Debug.LogError("[WeaponDeadzone] FAILED - Could not find WeaponHolder!");
        }
    }

    void LateUpdate()
    {
        if (!enableDeadzone || !initialized)
            return;

        if (weaponHolder.childCount == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("[WeaponDeadzone] WeaponHolder has no children!");
            return;
        }

        currentWeapon = weaponHolder.GetChild(0);
        if (currentWeapon == null) return;

        // Calculate deadzone offset from screen center
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector3 mousePos = Input.mousePosition;

        float offsetX = Mathf.Clamp((mousePos.x - screenCenter.x) / (Screen.width * 0.5f), -1f, 1f);
        float offsetY = Mathf.Clamp((mousePos.y - screenCenter.y) / (Screen.height * 0.5f), -1f, 1f);

        // Target rotation
        targetDeadzoneRotation = new Vector3(
            -offsetY * maxDeadzoneAngle,
            offsetX * maxDeadzoneAngle,
            0f
        );

        // Smooth interpolation
        currentDeadzoneRotation = Vector3.Lerp(
            currentDeadzoneRotation,
            targetDeadzoneRotation,
            Time.deltaTime * deadzoneSmoothing
        );

        // Apply deadzone rotation ADDITIVELY
        Quaternion currentRot = currentWeapon.localRotation;
        Quaternion deadzoneOffset = Quaternion.Euler(currentDeadzoneRotation);
        currentWeapon.localRotation = currentRot * deadzoneOffset;

        if (showDebugLogs && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[WeaponDeadzone] Active | Offset: ({offsetX:F2}, {offsetY:F2}) | Rotation: {currentDeadzoneRotation}");
        }
    }

    public void SetEnabled(bool enabled) => enableDeadzone = enabled;
    public bool IsEnabled() => enableDeadzone;
}
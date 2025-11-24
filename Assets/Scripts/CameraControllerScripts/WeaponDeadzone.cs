using UnityEngine;

/// <summary>
/// Makes weapon subtly follow mouse cursor within a deadzone
/// MUST run in LateUpdate AFTER CharacterAnimatorController to apply deadzone on top of other animations
/// Attach to Main Camera (same GameObject as WeaponManager)
/// </summary>
[DefaultExecutionOrder(100)] // Run after CharacterAnimatorController
public class WeaponDeadzone : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [Tooltip("Auto-finds WeaponHolder if not assigned")]
    [SerializeField] private Transform weaponHolder;

    [Tooltip("Auto-finds CharacterAnimatorController if not assigned")]
    [SerializeField] private CharacterAnimatorController characterAnimator;

    [Header("=== DEADZONE SETTINGS ===")]
    [Tooltip("Enable/disable deadzone effect")]
    [SerializeField] private bool enableDeadzone = true;

    [Tooltip("How much weapon rotates to follow mouse (degrees)")]
    [SerializeField] private float maxDeadzoneAngle = 5f;

    [Tooltip("How smoothly weapon follows cursor")]
    [SerializeField] private float deadzoneSmoothing = 10f;

    [Tooltip("Disable deadzone when aiming down sights")]
    [SerializeField] private bool disableDeadzoneOnADS = true;

    [Tooltip("How quickly weapon centers when entering ADS")]
    [SerializeField] private float adsCenteringSpeed = 15f;

    private Camera playerCamera;
    private Vector3 targetDeadzoneRotation;
    private Vector3 currentDeadzoneRotation;
    private Quaternion baseRotation; // Store rotation before applying deadzone

    void Start()
    {
        playerCamera = Camera.main;

        if (characterAnimator == null)
            characterAnimator = FindObjectOfType<CharacterAnimatorController>();

        if (weaponHolder == null)
        {
            GameObject holderObj = GameObject.Find("WeaponHolder");
            if (holderObj != null)
                weaponHolder = holderObj.transform;
        }

        Debug.Log($"[WeaponDeadzone] Initialized - Max Angle: {maxDeadzoneAngle}, Smoothing: {deadzoneSmoothing}");
    }

    void LateUpdate()
    {
        if (!enableDeadzone) return;

        if (weaponHolder == null || weaponHolder.childCount == 0)
            return;

        Transform weapon = weaponHolder.GetChild(0);
        bool isAiming = characterAnimator != null && characterAnimator.IsAiming();

        // Store the base rotation (from CharacterAnimatorController)
        baseRotation = weapon.localRotation;

        if (disableDeadzoneOnADS && isAiming)
        {
            // Center weapon when ADS
            currentDeadzoneRotation = Vector3.Lerp(currentDeadzoneRotation, Vector3.zero, Time.deltaTime * adsCenteringSpeed);
        }
        else
        {
            // Calculate deadzone rotation based on mouse position
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Vector3 mousePosition = Input.mousePosition;

            // Normalized offset from center (-1 to 1)
            float offsetX = (mousePosition.x - screenCenter.x) / (Screen.width / 2f);
            float offsetY = (mousePosition.y - screenCenter.y) / (Screen.height / 2f);

            // Clamp to prevent extreme angles
            offsetX = Mathf.Clamp(offsetX, -1f, 1f);
            offsetY = Mathf.Clamp(offsetY, -1f, 1f);

            // Target rotation based on cursor position
            targetDeadzoneRotation = new Vector3(
                -offsetY * maxDeadzoneAngle,  // Pitch (look up/down)
                offsetX * maxDeadzoneAngle,   // Yaw (look left/right)
                0f
            );

            // Smooth interpolation
            currentDeadzoneRotation = Vector3.Lerp(
                currentDeadzoneRotation,
                targetDeadzoneRotation,
                Time.deltaTime * deadzoneSmoothing
            );
        }

        // Apply deadzone rotation ON TOP OF base rotation
        Quaternion deadzoneRotation = Quaternion.Euler(currentDeadzoneRotation);
        weapon.localRotation = baseRotation * deadzoneRotation;
    }

    // Public API
    public void SetEnabled(bool enabled)
    {
        enableDeadzone = enabled;
    }

    public void SetMaxAngle(float angle)
    {
        maxDeadzoneAngle = angle;
    }

    public bool IsEnabled()
    {
        return enableDeadzone;
    }
}
using UnityEngine;

/// <summary>
/// Handles weapon visual animations: deadzone, sway, bob, breathing, tilt
/// Sends offsets to WeaponPositionController
/// Attach to Player GameObject
/// </summary>
public class CharacterAnimatorController : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private ScopeManager scopeManager;
    [SerializeField] private WeaponPositionController weaponPositionController;

    [Header("=== WEAPON DEADZONE ===")]
    [SerializeField] private bool enableDeadzone = true;
    [SerializeField] private float maxDeadzoneAngle = 5f;
    [SerializeField] private float deadzoneSmoothing = 10f;
    [Range(0f, 1f)]
    [SerializeField] private float adsDeadzoneMultiplier = 0.2f;

    [Header("=== WEAPON SWAY ===")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float swaySmoothing = 6f;
    [SerializeField] private float adsSwayMultiplier = 0.3f;

    [Header("=== WEAPON BOB ===")]
    [SerializeField] private bool enableBob = true;
    [SerializeField] private float bobAmount = 0.02f;
    [SerializeField] private float bobSpeed = 10f;

    [Header("=== BREATHING ===")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathingAmount = 0.001f;
    [SerializeField] private float breathingSpeed = 2f;

    [Header("=== WEAPON TILT ===")]
    [SerializeField] private bool enableTilt = true;
    [SerializeField] private float tiltAmount = 2f;

    [Header("=== RECOIL REDUCTION ===")]
    [SerializeField] private float adsRecoilReduction = 0.5f;

    private float bobTimer = 0f;
    private float breathTimer = 0f;

    private Vector3 currentDeadzoneRotation;
    private Vector3 targetDeadzoneRotation;
    private Vector3 swayPosition;

    void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (scopeManager == null)
            scopeManager = FindFirstObjectByType<ScopeManager>();

        if (weaponPositionController == null)
            weaponPositionController = GetComponent<WeaponPositionController>();
    }

    void LateUpdate()
    {
        CalculateAndApplyOffsets();
    }

    bool IsAiming()
    {
        if (scopeManager != null)
            return scopeManager.IsADS();
        return Input.GetKey(KeyCode.Mouse1);
    }

    void CalculateAndApplyOffsets()
    {
        bool isAiming = IsAiming();
        bool isSprinting = playerMovement != null && playerMovement.IsSprinting();

        // === POSITION OFFSET ===
        Vector3 positionOffset = Vector3.zero;

        // Mouse Sway (reduced when ADS, disabled when sprinting)
        if (enableSway && !isSprinting)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float swayMult = isAiming ? adsSwayMultiplier : 1f;
            Vector3 targetSway = new Vector3(-mouseY, mouseX, 0f) * swayAmount * swayMult;
            swayPosition = Vector3.Lerp(swayPosition, targetSway, Time.deltaTime * swaySmoothing);
            positionOffset += swayPosition;
        }

        // Movement Bob (disabled when ADS or sprinting)
        if (enableBob && !isAiming && !isSprinting)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            bool isMoving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;

            if (isMoving)
            {
                bobTimer += Time.deltaTime * bobSpeed;
                positionOffset += new Vector3(
                    Mathf.Sin(bobTimer) * bobAmount,
                    Mathf.Sin(bobTimer * 2f) * bobAmount,
                    0f
                );
            }
            else
            {
                bobTimer = 0f;
            }
        }

        // Breathing (reduced when ADS)
        if (enableBreathing && !isSprinting)
        {
            breathTimer += Time.deltaTime * breathingSpeed;
            float breathMult = isAiming ? 0.5f : 1f;
            positionOffset += new Vector3(
                Mathf.Sin(breathTimer) * breathingAmount * breathMult,
                Mathf.Sin(breathTimer * 0.5f) * breathingAmount * breathMult,
                0f
            );
        }

        // === ROTATION OFFSET ===
        Vector3 rotationOffset = Vector3.zero;

        // Deadzone (reduced when ADS, disabled when sprinting)
        if (enableDeadzone && !isSprinting)
        {
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            Vector3 mousePos = Input.mousePosition;

            float offsetX = Mathf.Clamp((mousePos.x - screenCenter.x) / (Screen.width * 0.5f), -1f, 1f);
            float offsetY = Mathf.Clamp((mousePos.y - screenCenter.y) / (Screen.height * 0.5f), -1f, 1f);

            float effectiveAngle = maxDeadzoneAngle * (isAiming ? adsDeadzoneMultiplier : 1f);

            targetDeadzoneRotation = new Vector3(
                -offsetY * effectiveAngle,
                offsetX * effectiveAngle,
                0f
            );

            currentDeadzoneRotation = Vector3.Lerp(
                currentDeadzoneRotation,
                targetDeadzoneRotation,
                Time.deltaTime * deadzoneSmoothing
            );

            rotationOffset += currentDeadzoneRotation;
        }

        // Strafe Tilt (disabled when ADS or sprinting)
        if (enableTilt && !isAiming && !isSprinting)
        {
            float h = Input.GetAxis("Horizontal");
            rotationOffset.z += -h * tiltAmount;
        }

        // Send offsets to WeaponPositionController
        if (weaponPositionController != null)
        {
            weaponPositionController.SetAnimationOffset(positionOffset, rotationOffset);
        }
    }

    // === PUBLIC API ===
    public bool IsADS() => IsAiming();
    public float GetADSRecoilMultiplier() => IsAiming() ? adsRecoilReduction : 1f;
    public Vector3 GetDeadzoneRotation() => currentDeadzoneRotation;
}
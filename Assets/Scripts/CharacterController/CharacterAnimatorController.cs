using UnityEngine;

/// <summary>
/// Handles all weapon visual animations: ADS, sway, breathing, bob, tilt, sprint
/// Attach to Player GameObject
/// </summary>
public class CharacterAnimatorController : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [Tooltip("The transform that holds the weapon (where WeaponManager spawns weapons)")]
    public Transform weaponHolder;

    [SerializeField] private PlayerMovement playerMovement;

    [Header("=== ADS SETTINGS ===")]
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
    [SerializeField] private float adsSpeed = 8f;
    [SerializeField] private float adsFOV = 50f;
    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float adsRecoilReduction = 0.5f;

    [Header("=== WEAPON SWAY ===")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float swaySmoothing = 6f;
    [SerializeField] private float adsSwayMultiplier = 0.3f;

    [Header("=== WEAPON BOB ===")]
    [SerializeField] private float bobAmount = 0.02f;
    [SerializeField] private float bobSpeed = 10f;

    [Header("=== BREATHING ===")]
    [SerializeField] private float breathingAmount = 0.001f;
    [SerializeField] private float breathingSpeed = 2f;

    [Header("=== WEAPON TILT ===")]
    [SerializeField] private float tiltAmount = 2f;
    [SerializeField] private float tiltSpeed = 5f;

    private Camera playerCamera;
    private bool isAiming = false;
    private float bobTimer = 0f;
    private float breathTimer = 0f;

    private Vector3 swayPosition;
    private Vector3 targetWeaponPosition;
    private Quaternion targetWeaponRotation;

    void Start()
    {
        playerCamera = Camera.main;

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (weaponHolder == null)
        {
            // Try to find WeaponHolder under camera
            Transform camTransform = playerCamera != null ? playerCamera.transform : transform;
            weaponHolder = camTransform.Find("WeaponHolder");

            if (weaponHolder == null)
            {
                Debug.LogError("[CharacterAnimatorController] weaponHolder not assigned and couldn't find WeaponHolder!");
            }
        }
    }

    void Update()
    {
        HandleADS();

        if (weaponHolder != null && weaponHolder.childCount > 0)
        {
            Transform weapon = weaponHolder.GetChild(0);
            UpdateWeaponAnimations(weapon);
        }
    }

    void HandleADS()
    {
        isAiming = Input.GetKey(aimKey);

        if (playerCamera != null)
        {
            float targetFOV = isAiming ? adsFOV : defaultFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);
        }
    }

    void UpdateWeaponAnimations(Transform weapon)
    {
        // Mouse sway
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float swayMultiplier = isAiming ? adsSwayMultiplier : 1f;
        Vector3 targetSway = new Vector3(-mouseY, mouseX, 0f) * swayAmount * swayMultiplier;
        swayPosition = Vector3.Lerp(swayPosition, targetSway, Time.deltaTime * swaySmoothing);

        // Movement bob - detect movement via input instead
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);

        if (isMoving && !isAiming)
        {
            bobTimer += Time.deltaTime * bobSpeed;
        }
        else
        {
            bobTimer = 0f;
        }

        Vector3 bobOffset = Vector3.zero;
        if (isMoving && !isAiming)
        {
            bobOffset = new Vector3(
                Mathf.Sin(bobTimer) * bobAmount,
                Mathf.Sin(bobTimer * 2f) * bobAmount,
                0f
            );
        }

        // Breathing
        breathTimer += Time.deltaTime * breathingSpeed;
        float breathMultiplier = isAiming ? 0.5f : 1f;
        Vector3 breathOffset = new Vector3(
            Mathf.Sin(breathTimer) * breathingAmount * breathMultiplier,
            Mathf.Sin(breathTimer * 0.5f) * breathingAmount * breathMultiplier,
            0f
        );

        // Weapon tilt on strafe
        float tiltAngle = 0f;
        if (!isAiming)
        {
            tiltAngle = -horizontal * tiltAmount;
        }

        // Apply all animations
        targetWeaponPosition = swayPosition + bobOffset + breathOffset;
        targetWeaponRotation = Quaternion.Euler(0f, 0f, tiltAngle);

        weapon.localPosition = Vector3.Lerp(weapon.localPosition, targetWeaponPosition, Time.deltaTime * swaySmoothing);
        weapon.localRotation = Quaternion.Slerp(weapon.localRotation, targetWeaponRotation, Time.deltaTime * tiltSpeed);
    }

    public bool IsAiming()
    {
        return isAiming;
    }

    public float GetADSRecoilMultiplier()
    {
        return isAiming ? adsRecoilReduction : 1f;
    }
}
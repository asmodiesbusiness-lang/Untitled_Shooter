using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSCharacterController : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private CameraRecoil cameraRecoil;
    [SerializeField] private CharacterAnimatorController animatorController;

    [Header("=== LOOK SETTINGS ===")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;

    [Header("=== LEAN SETTINGS ===")]
    [SerializeField] private float leanAngle = 15f;
    [SerializeField] private float leanSpeed = 10f;

    private float verticalRotation = 0f;
    private float currentLeanAngle = 0f;
    private float targetLeanAngle = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (weaponManager == null)
            weaponManager = GetComponentInChildren<WeaponManager>();

        if (cameraRecoil == null && playerCamera != null)
            cameraRecoil = playerCamera.GetComponent<CameraRecoil>();

        if (animatorController == null)
            animatorController = GetComponent<CharacterAnimatorController>();
    }

    void Update()
    {
        HandleLook();
        HandleLean();
        HandleWeaponInput();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Horizontal rotation on player body
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation on camera
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

        // Apply camera rotation (includes lean)
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, currentLeanAngle);

        // Update recoil system
        if (cameraRecoil != null)
            cameraRecoil.UpdateRotation(verticalRotation, transform.eulerAngles.y);
    }

    void HandleLean()
    {
        bool leanLeft = Input.GetKey(KeyCode.Q);
        bool leanRight = Input.GetKey(KeyCode.E);

        if (leanLeft && !leanRight)
            targetLeanAngle = leanAngle;
        else if (leanRight && !leanLeft)
            targetLeanAngle = -leanAngle;
        else
            targetLeanAngle = 0f;

        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle, leanSpeed * Time.deltaTime);
    }

    void HandleWeaponInput()
    {
        if (weaponManager == null) return;

        WeaponController currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon == null) return;

        // Shoot
        if (Input.GetMouseButton(0))
            currentWeapon.TryShoot();

        // Reload
        if (Input.GetKeyDown(KeyCode.R))
            currentWeapon.Reload();
    }

    // === PUBLIC API ===

    public Camera GetPlayerCamera() => playerCamera;

    public CameraRecoil GetCameraRecoil() => cameraRecoil;

    public bool IsAiming()
    {
        return animatorController != null && animatorController.IsADS();
    }

    public void AddAmmo(int amount)
    {
        if (weaponManager != null)
        {
            WeaponController weapon = weaponManager.GetCurrentWeapon();
            if (weapon != null)
                weapon.AddReserveAmmo(amount);
        }
    }
}
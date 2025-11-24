using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private CameraRecoil cameraRecoil; // NEW

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 80f;

    [Header("Lean Settings")]
    [SerializeField] private float leanAngle = 20f;
    [SerializeField] private float leanSpeed = 10f;

    private float verticalRotation = 0f;
    private float currentLeanAngle = 0f;
    private float targetLeanAngle = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (weaponManager == null)
        {
            weaponManager = GetComponentInChildren<WeaponManager>();
            if (weaponManager == null)
            {
                Debug.LogError("FPSCharacterController: No WeaponManager found!");
            }
        }

        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("FPSCharacterController: No camera assigned or found!");
            }
        }

        // Auto-find camera recoil
        if (cameraRecoil == null && playerCamera != null)
        {
            cameraRecoil = playerCamera.GetComponent<CameraRecoil>();
        }
    }

    void Update()
    {
        HandleLook();
        HandleWeaponInput();
    }

    void HandleLook()
    {
        // Mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player body horizontally
        transform.Rotate(Vector3.up * mouseX);

        // Calculate vertical rotation
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

        // Handle lean input
        bool leanLeft = Input.GetKey(KeyCode.E);
        bool leanRight = Input.GetKey(KeyCode.Q);

        if (leanLeft && !leanRight)
        {
            targetLeanAngle = -leanAngle;
        }
        else if (leanRight && !leanLeft)
        {
            targetLeanAngle = leanAngle;
        }
        else
        {
            targetLeanAngle = 0f;
        }

        // Smoothly interpolate lean
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle, leanSpeed * Time.deltaTime);

        // Apply BOTH vertical look AND lean to camera in one rotation
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, currentLeanAngle);

        // Update camera recoil system with current rotation
        if (cameraRecoil != null)
        {
            cameraRecoil.UpdateRotation(verticalRotation, transform.eulerAngles.y);
        }
    }

    void HandleWeaponInput()
    {
        if (weaponManager == null) return;

        WeaponController currentWeapon = weaponManager.GetCurrentWeapon();
        if (currentWeapon == null) return;

        // Shooting
        if (Input.GetMouseButton(0))
        {
            currentWeapon.TryShoot();
        }

        // Reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentWeapon.Reload();
        }
    }

    public Camera GetPlayerCamera()
    {
        return playerCamera;
    }

    public bool IsAiming()
    {
        // Query the CharacterAnimatorController for ADS state instead of tracking locally
        CharacterAnimatorController animator = GetComponent<CharacterAnimatorController>();
        return animator != null ? animator.IsAiming() : false;
    }

    public void AddAmmo(int amount)
    {
        if (weaponManager != null)
        {
            WeaponController currentWeapon = weaponManager.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                currentWeapon.AddReserveAmmo(amount);
            }
        }
    }

    /// <summary>
    /// Get the camera recoil component (for weapon to apply recoil)
    /// </summary>
    public CameraRecoil GetCameraRecoil()
    {
        return cameraRecoil;
    }
}
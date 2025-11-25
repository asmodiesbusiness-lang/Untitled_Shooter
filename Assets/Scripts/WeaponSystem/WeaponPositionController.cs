using UnityEngine;

/// <summary>
/// Handles weapon position states (hip, ADS, sprint) with smooth transitions
/// Reads values from WeaponData, applies to weapon transform
/// Attach to Player GameObject
/// </summary>
public class WeaponPositionController : MonoBehaviour
{
    public enum WeaponState { Hip, ADS, Sprint }

    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private ScopeManager scopeManager;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("=== TRANSITION SETTINGS ===")]
    [SerializeField] private float positionSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float sprintTransitionSpeed = 8f;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = false;

    // State
    private WeaponState currentState = WeaponState.Hip;
    private WeaponData currentWeaponData;

    // Current transform values (smoothly interpolated)
    private Vector3 currentPosition;
    private Quaternion currentRotation;

    // Target transform values
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    // Animation offset (from CharacterAnimatorController)
    private Vector3 animationPositionOffset;
    private Vector3 animationRotationOffset;

    void Start()
    {
        if (weaponHolder == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                weaponHolder = cam.transform.Find("WeaponHolder");
        }

        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();

        if (scopeManager == null)
            scopeManager = FindFirstObjectByType<ScopeManager>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        UpdateWeaponData();
        UpdateState();
        UpdateTargetTransform();
    }

    void LateUpdate()
    {
        ApplyTransform();
    }

    void UpdateWeaponData()
    {
        if (weaponManager == null) return;

        WeaponController wc = weaponManager.GetCurrentWeapon();
        if (wc != null)
        {
            WeaponData newData = wc.GetWeaponData();
            if (newData != currentWeaponData)
            {
                currentWeaponData = newData;
                // Snap to hip position on weapon change
                if (currentWeaponData != null)
                {
                    currentPosition = currentWeaponData.hipFirePosition.position;
                    currentRotation = Quaternion.Euler(currentWeaponData.hipFirePosition.rotation);
                }
            }
        }
    }

    void UpdateState()
    {
        // Priority: Sprint > ADS > Hip
        bool isSprinting = playerMovement != null && playerMovement.IsSprinting();
        bool isAiming = scopeManager != null && scopeManager.IsADS();

        if (isSprinting && !isAiming)
            currentState = WeaponState.Sprint;
        else if (isAiming)
            currentState = WeaponState.ADS;
        else
            currentState = WeaponState.Hip;
    }

    void UpdateTargetTransform()
    {
        if (currentWeaponData == null) return;

        TransformOffset targetOffset;

        switch (currentState)
        {
            case WeaponState.ADS:
                targetOffset = currentWeaponData.adsPosition;
                break;
            case WeaponState.Sprint:
                targetOffset = currentWeaponData.sprintPosition;
                break;
            case WeaponState.Hip:
            default:
                targetOffset = currentWeaponData.hipFirePosition;
                break;
        }

        targetPosition = targetOffset.position;
        targetRotation = Quaternion.Euler(targetOffset.rotation);
    }

    void ApplyTransform()
    {
        if (weaponHolder == null || weaponHolder.childCount == 0) return;

        Transform weapon = weaponHolder.GetChild(0);
        if (weapon == null) return;

        // Determine transition speed based on state
        float posSpeed = currentState == WeaponState.Sprint ? sprintTransitionSpeed : positionSpeed;
        float rotSpeed = currentState == WeaponState.Sprint ? sprintTransitionSpeed : rotationSpeed;

        // Smooth interpolation
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * posSpeed);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * rotSpeed);

        // Apply base position + animation offsets
        weapon.localPosition = currentPosition + animationPositionOffset;
        weapon.localRotation = currentRotation * Quaternion.Euler(animationRotationOffset);

        if (showDebugInfo && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[WeaponPosition] State: {currentState} | Pos: {currentPosition} | Target: {targetPosition}");
        }
    }

    // === PUBLIC API ===

    /// <summary>
    /// Called by CharacterAnimatorController to add animation offsets (sway, bob, deadzone)
    /// </summary>
    public void SetAnimationOffset(Vector3 positionOffset, Vector3 rotationOffset)
    {
        animationPositionOffset = positionOffset;
        animationRotationOffset = rotationOffset;
    }

    public WeaponState GetCurrentState() => currentState;
    public bool IsAiming() => currentState == WeaponState.ADS;
    public bool IsSprinting() => currentState == WeaponState.Sprint;

    /// <summary>
    /// Force snap to a position (no lerp)
    /// </summary>
    public void SnapToState(WeaponState state)
    {
        if (currentWeaponData == null) return;

        currentState = state;
        TransformOffset offset;

        switch (state)
        {
            case WeaponState.ADS:
                offset = currentWeaponData.adsPosition;
                break;
            case WeaponState.Sprint:
                offset = currentWeaponData.sprintPosition;
                break;
            default:
                offset = currentWeaponData.hipFirePosition;
                break;
        }

        currentPosition = offset.position;
        currentRotation = Quaternion.Euler(offset.rotation);
        targetPosition = currentPosition;
        targetRotation = currentRotation;
    }
}
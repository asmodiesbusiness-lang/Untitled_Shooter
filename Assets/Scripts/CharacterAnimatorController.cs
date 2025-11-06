using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized procedural animation controller for character
/// Handles: Weapon recoil, weapon sway, held item animations, player hand animations
/// All procedural motion is calculated and applied from this single script
/// NOW WITH: Base ADS System + Weapon Offsets, Dual/Multi-Point Recoil System
/// </summary>
public class CharacterAnimatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("=== ADS (AIM DOWN SIGHTS) SYSTEM ===")]
    [Tooltip("Base ADS position - each weapon adds its offset to this")]
    public Vector3 baseAdsPosition = new Vector3(0f, -0.05f, 0.35f);
    [Tooltip("Default ADS speed if weapon doesn't override")]
    public float defaultAdsSpeed = 10f;
    [Tooltip("Default ADS FOV if weapon doesn't override")]
    public float defaultAdsFOV = 50f;

    [SerializeField] private Vector3 adsPosition = new Vector3(0f, -0.05f, 0.35f);
    [SerializeField] private float adsSpeed = 10f;
    [SerializeField] private float adsFOV = 50f;
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float adsRecoilMultiplier = 0.5f;
    [SerializeField] private float adsSwayMultiplier = 0.3f;

    [Header("=== WEAPON RECOIL SYSTEM ===")]
    [Header("Camera Recoil (Affects Aim)")]
    [SerializeField] private float cameraRecoilSnapSpeed = 15f;
    [SerializeField] private float cameraRecoilReturnSpeed = 8f;
    [SerializeField] private AnimationCurve cameraRecoveryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float maxRecoilAccumulation = 10f;

    [Header("Weapon Visual Recoil (What You SEE)")]
    [SerializeField] private float weaponVisualMultiplier = 4f;
    [SerializeField] private float weaponSnapSpeed = 25f;
    [SerializeField] private float weaponReturnSpeed = 5f;
    [SerializeField] private float weaponPositionKickback = 0.15f;

    [Header("Recoil Feel Preset")]
    [SerializeField] private RecoilFeelPreset recoilPreset = RecoilFeelPreset.Balanced;

    [Header("=== DUAL/MULTI-POINT RECOIL SYSTEM ===")]
    [Tooltip("Enable the new dual-point recoil system")]
    [SerializeField] private bool useDualPointRecoil = true;
    [Tooltip("Show recoil point gizmos in editor")]
    [SerializeField] private bool showRecoilPointGizmos = true;
    [Tooltip("Size of gizmo spheres")]
    [SerializeField] private float gizmoSize = 0.02f;

    [Header("=== WEAPON SWAY SYSTEM ===")]
    [Header("Mouse Look Sway")]
    [SerializeField] private float swayAmount = 0.02f;
    [SerializeField] private float maxSwayAmount = 0.06f;
    [SerializeField] private float swaySmoothness = 6f;

    [Header("Movement Bob")]
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobAmount = 0.08f;
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Movement Sway")]
    [SerializeField] private float walkSwayAmount = 0.03f;
    [SerializeField] private float runSwayAmount = 0.05f;

    [Header("Weapon Tilt")]
    [SerializeField] private float tiltAmount = 2f;
    [SerializeField] private float runTiltMultiplier = 1.5f;
    [SerializeField] private float tiltSmoothness = 5f;

    [Header("Breathing Idle")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathingAmplitude = 0.005f;
    [SerializeField] private float breathingSpeed = 1.5f;

    [Header("Hold Breath System")]
    [Tooltip("Key to hold breath (default X)")]
    [SerializeField] private KeyCode holdBreathKey = KeyCode.X;
    [Tooltip("Maximum time player can hold breath (seconds)")]
    [SerializeField] private float maxBreathHoldTime = 5f;
    [Tooltip("Cooldown before breath recovers fully (seconds)")]
    [SerializeField] private float breathRecoveryTime = 3f;
    [Tooltip("Weapon sway reduction while holding breath (0 = no reduction, 1 = no sway)")]
    [SerializeField] private float breathStabilizationAmount = 0.7f;
    [Tooltip("View bob reduction while holding breath (0 = no reduction, 1 = no bob)")]
    [SerializeField] private float breathBobReduction = 0.8f;
    [Tooltip("Sway multiplier after running out of breath (2 = double sway)")]
    [SerializeField] private float exhaustedSwayMultiplier = 3f;
    [Tooltip("How long excessive sway lasts after exhaustion")]
    [SerializeField] private float exhaustedSwayDuration = 2f;

    [Header("Landing Impact")]
    [SerializeField] private float landingImpact = 0.15f;
    [SerializeField] private float landingRecoverySpeed = 5f;

    [Header("Sprint Behavior")]
    [SerializeField] private float sprintForwardAmount = 0.1f;
    [SerializeField] private float sprintDownAmount = 0.05f;
    [SerializeField] private float sprintTransitionSpeed = 4f;

    [Header("=== HELD ITEM SWAY (Future) ===")]
    [SerializeField] private Transform heldItemTransform;
    [SerializeField] private bool enableItemSway = true;
    [SerializeField] private float itemSwayMultiplier = 1.5f;

    // ADS state
    private bool isAiming = false;
    private Vector3 currentWeaponPosition;
    private float currentFOV;

    // Recoil state
    private Vector3 currentCameraRecoil;
    private Vector3 targetCameraRecoil;
    private Vector3 weaponRotationRecoil;
    private Vector3 weaponPositionRecoil;
    private float recoilRecoveryProgress = 1f;

    // Sway state
    private Vector3 originalWeaponPosition;
    private Quaternion originalWeaponRotation;
    private Vector3 swayPosition;
    private Vector3 swayRotation;
    private float bobTimer = 0f;
    private float breathingTimer = 0f;
    private float currentTilt = 0f;
    private float landingDip = 0f;
    private bool wasGrounded = true;
    private bool wasRolling = false;
    private float sprintProgress = 0f;

    // Hold breath state
    private bool isHoldingBreath = false;
    private float currentBreathHoldTime = 0f;
    private float breathRecoveryTimer = 0f;
    private bool isExhausted = false;
    private float exhaustedTimer = 0f;

    // Held item state
    private Vector3 originalItemPosition;
    private Quaternion originalItemRotation;

    // Current weapon data
    private WeaponData currentWeaponData;

    // Dual recoil point system
    private List<Transform> recoilPointTransforms = new List<Transform>();
    private List<Quaternion> recoilPointOriginalRotations = new List<Quaternion>();
    private List<Vector3> recoilPointTargetRotations = new List<Vector3>(); // Target rotations (euler) that accumulate like camera recoil

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (weaponManager == null)
        {
            weaponManager = GetComponentInChildren<WeaponManager>();
        }

        currentFOV = normalFOV;
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = currentFOV;
        }

        // Initialize ADS position to base
        adsPosition = baseAdsPosition;
    }

    void Update()
    {
        HandleHoldBreath();
        HandleADS();
        UpdateWeaponRecoil();
        UpdateWeaponSway();
        UpdateHeldItemSway();
        UpdateDualRecoilPoints();
    }

    // ========================
    // ADS SYSTEM
    // ========================

    void HandleADS()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isAiming = false;
        }

        Vector3 targetPos = isAiming ? adsPosition : originalWeaponPosition;
        currentWeaponPosition = Vector3.Lerp(currentWeaponPosition, targetPos, adsSpeed * Time.deltaTime);

        float targetFOV = isAiming ? adsFOV : normalFOV;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, adsSpeed * Time.deltaTime);

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = currentFOV;
        }
    }

    public bool IsAiming()
    {
        return isAiming;
    }

    // ========================
    // HOLD BREATH SYSTEM
    // ========================

    void HandleHoldBreath()
    {
        // Check if current weapon allows hold breath
        bool canHoldBreath = true;
        if (currentWeaponData != null)
        {
            canHoldBreath = currentWeaponData.allowHoldBreath;
        }

        // Check if holding breath key
        if (Input.GetKey(holdBreathKey) && !isExhausted && canHoldBreath)
        {
            isHoldingBreath = true;
            currentBreathHoldTime += Time.deltaTime;

            // Check if ran out of breath
            if (currentBreathHoldTime >= maxBreathHoldTime)
            {
                isHoldingBreath = false;
                isExhausted = true;
                exhaustedTimer = 0f;
                currentBreathHoldTime = maxBreathHoldTime; // Cap it
            }
        }
        else
        {
            isHoldingBreath = false;

            // Recover breath
            if (currentBreathHoldTime > 0f)
            {
                breathRecoveryTimer += Time.deltaTime;
                float recoverySpeed = maxBreathHoldTime / breathRecoveryTime;
                currentBreathHoldTime -= recoverySpeed * Time.deltaTime;
                currentBreathHoldTime = Mathf.Max(0f, currentBreathHoldTime);
            }
            else
            {
                breathRecoveryTimer = 0f;
            }
        }

        // Handle exhaustion
        if (isExhausted)
        {
            exhaustedTimer += Time.deltaTime;
            if (exhaustedTimer >= exhaustedSwayDuration)
            {
                isExhausted = false;
                exhaustedTimer = 0f;
            }
        }
    }

    public bool IsHoldingBreath()
    {
        return isHoldingBreath;
    }

    public float GetBreathPercentage()
    {
        return 1f - (currentBreathHoldTime / maxBreathHoldTime);
    }

    public bool IsExhausted()
    {
        return isExhausted;
    }

    /// <summary>
    /// Call this when equipping a new weapon to update ADS values
    /// </summary>
    public void SetWeaponData(WeaponData weaponData)
    {
        currentWeaponData = weaponData;

        if (weaponData != null)
        {
            // Calculate final ADS position: base + weapon offset
            adsPosition = baseAdsPosition + weaponData.adsPositionOffset;

            // Use weapon overrides if set, otherwise use defaults
            adsSpeed = weaponData.adsSpeedOverride > 0 ? weaponData.adsSpeedOverride : defaultAdsSpeed;
            adsFOV = weaponData.adsFOVOverride > 0 ? weaponData.adsFOVOverride : defaultAdsFOV;
        }
        else
        {
            // Reset to defaults
            adsPosition = baseAdsPosition;
            adsSpeed = defaultAdsSpeed;
            adsFOV = defaultAdsFOV;
        }

        // Setup recoil points
        SetupRecoilPoints();
    }

    // ========================
    // DUAL RECOIL POINT SYSTEM
    // ========================

    void SetupRecoilPoints()
    {
        // Clear existing points
        ClearRecoilPoints();

        if (currentWeaponData == null || !useDualPointRecoil)
            return;

        if (currentWeaponData.recoilPoints == null || currentWeaponData.recoilPoints.Count == 0)
        {
            // No recoil points defined, will use standard single-point recoil
            Debug.LogWarning($"[Recoil] Dual Point Recoil enabled but {currentWeaponData.weaponName} has no recoil points defined! Using standard recoil instead. Add recoil points in WeaponData to use dual-point system.");
            return;
        }

        // Create transform for each recoil point
        foreach (RecoilPoint recoilPoint in currentWeaponData.recoilPoints)
        {
            GameObject pointObj = new GameObject($"RecoilPoint_{recoilPoint.pointName}");
            pointObj.transform.SetParent(weaponPivot);
            pointObj.transform.localPosition = recoilPoint.positionOffset;
            pointObj.transform.localRotation = Quaternion.identity;

            recoilPointTransforms.Add(pointObj.transform);
            recoilPointOriginalRotations.Add(Quaternion.identity);
            recoilPointTargetRotations.Add(Vector3.zero); // Start with no target rotation
        }

        // CRITICAL: Parent weapon visuals to first recoil point (usually barrel)
        if (recoilPointTransforms.Count > 0 && weaponPivot != null)
        {
            Transform primaryPoint = recoilPointTransforms[0]; // First point (barrel)

            // Find all weapon models/meshes under weaponPivot
            List<Transform> weaponChildren = new List<Transform>();
            foreach (Transform child in weaponPivot)
            {
                // Skip recoil points themselves
                if (!child.name.StartsWith("RecoilPoint_"))
                {
                    weaponChildren.Add(child);
                }
            }

            // Parent weapon models to primary recoil point
            foreach (Transform weaponChild in weaponChildren)
            {
                // Store world position/rotation
                Vector3 worldPos = weaponChild.position;
                Quaternion worldRot = weaponChild.rotation;
                Vector3 worldScale = weaponChild.lossyScale;

                // Reparent
                weaponChild.SetParent(primaryPoint);

                // Restore world position/rotation (maintains visual position)
                weaponChild.position = worldPos;
                weaponChild.rotation = worldRot;

                Debug.Log($"[Recoil] Parented {weaponChild.name} to {primaryPoint.name}");
            }
        }

        Debug.Log($"[Recoil] Setup {recoilPointTransforms.Count} recoil points for {currentWeaponData.weaponName}: {string.Join(", ", currentWeaponData.recoilPoints.ConvertAll(p => p.pointName))}");
    }

    void ClearRecoilPoints()
    {
        foreach (Transform point in recoilPointTransforms)
        {
            if (point != null)
                Destroy(point.gameObject);
        }

        recoilPointTransforms.Clear();
        recoilPointOriginalRotations.Clear();
        recoilPointTargetRotations.Clear(); // Clear targets too
    }

    void UpdateDualRecoilPoints()
    {
        if (!useDualPointRecoil || recoilPointTransforms.Count == 0)
            return;

        // Work like camera recoil: snap to target, then recover target
        for (int i = 0; i < recoilPointTransforms.Count; i++)
        {
            if (recoilPointTransforms[i] != null && i < recoilPointTargetRotations.Count)
            {
                // Phase 1: Snap current rotation to target (like camera snap)
                Quaternion targetRot = Quaternion.Euler(recoilPointTargetRotations[i]);
                recoilPointTransforms[i].localRotation = Quaternion.Slerp(
                    recoilPointTransforms[i].localRotation,
                    targetRot,
                    cameraRecoilSnapSpeed * Time.deltaTime // Use camera snap speed for consistency
                );

                // Phase 2: Recover target back to zero (like camera recovery)
                if (recoilPointTargetRotations[i].magnitude > 0.01f)
                {
                    recoilPointTargetRotations[i] = Vector3.Lerp(
                        recoilPointTargetRotations[i],
                        Vector3.zero,
                        weaponReturnSpeed * Time.deltaTime
                    );
                }
                else
                {
                    recoilPointTargetRotations[i] = Vector3.zero;
                }
            }
        }
    }

    void ApplyDualPointRecoil(Vector3 recoilRotation)
    {
        if (!useDualPointRecoil || recoilPointTransforms.Count == 0 || currentWeaponData == null)
            return;

        // Apply recoil to each point's TARGET (like camera recoil)
        for (int i = 0; i < recoilPointTransforms.Count && i < currentWeaponData.recoilPoints.Count && i < recoilPointTargetRotations.Count; i++)
        {
            RecoilPoint recoilPoint = currentWeaponData.recoilPoints[i];

            // Calculate recoil for this specific point
            // X = Pitch (up/down) - barrel should have high X for upward kick
            // Y = Yaw (left/right) - controls horizontal spread
            // Z = Roll (kickback) - stock should have high Z for backward kick
            Vector3 baseRecoil = recoilRotation * recoilPoint.influence;
            Vector3 pointRecoil = Vector3.Scale(baseRecoil, recoilPoint.rotationMultiplier);

            // Add to target rotation (accumulates like camera recoil)
            Vector3 newTarget = recoilPointTargetRotations[i] + pointRecoil;

            // Clamp to prevent infinite accumulation
            newTarget.x = Mathf.Clamp(newTarget.x, -45f, 45f);
            newTarget.y = Mathf.Clamp(newTarget.y, -45f, 45f);
            newTarget.z = Mathf.Clamp(newTarget.z, -45f, 45f);

            // Assign back to list
            recoilPointTargetRotations[i] = newTarget;
        }
    }

    // ========================
    // WEAPON RECOIL SYSTEM
    // ========================

    void UpdateWeaponRecoil()
    {
        if (playerCamera == null) return;

        float cameraSnap = cameraRecoilSnapSpeed;
        float cameraReturn = cameraRecoilReturnSpeed;
        float weaponReturn = weaponReturnSpeed;

        switch (recoilPreset)
        {
            case RecoilFeelPreset.Realistic:
                cameraSnap *= 1.3f;
                cameraReturn *= 0.8f;
                weaponReturn *= 0.6f;
                break;
            case RecoilFeelPreset.Arcade:
                cameraSnap *= 0.7f;
                cameraReturn *= 1.5f;
                weaponReturn *= 1.2f;
                break;
            case RecoilFeelPreset.Heavy:
                cameraSnap *= 0.5f;
                cameraReturn *= 0.5f;
                weaponReturn *= 0.3f;
                break;
        }

        currentCameraRecoil = Vector3.Lerp(currentCameraRecoil, targetCameraRecoil, cameraSnap * Time.deltaTime);

        if (targetCameraRecoil.magnitude > 0.01f)
        {
            recoilRecoveryProgress = 0f;
        }

        recoilRecoveryProgress = Mathf.Clamp01(recoilRecoveryProgress + (cameraReturn * Time.deltaTime * 0.5f));
        float curveValue = cameraRecoveryCurve.Evaluate(recoilRecoveryProgress);
        targetCameraRecoil = Vector3.Lerp(targetCameraRecoil, Vector3.zero, curveValue * Time.deltaTime * cameraReturn);

        playerCamera.transform.localRotation *= Quaternion.Euler(currentCameraRecoil * Time.deltaTime);

        if (weaponPivot != null)
        {
            weaponRotationRecoil = Vector3.Lerp(weaponRotationRecoil, Vector3.zero, weaponReturn * Time.deltaTime);
            weaponPositionRecoil = Vector3.Lerp(weaponPositionRecoil, Vector3.zero, weaponReturn * Time.deltaTime);
        }
    }

    public void ApplyRecoil(float recoilX, float recoilY)
    {
        float recoilMod = isAiming ? adsRecoilMultiplier : 1f;

        float randomHorizontal = Random.Range(-recoilX, recoilX) * recoilMod;
        float verticalRecoil = recoilY * recoilMod;

        targetCameraRecoil += new Vector3(-verticalRecoil, randomHorizontal, 0);
        targetCameraRecoil.x = Mathf.Clamp(targetCameraRecoil.x, -maxRecoilAccumulation, 0);

        if (weaponPivot != null)
        {
            float visualMultiplier = weaponVisualMultiplier;

            if (recoilPreset == RecoilFeelPreset.Heavy)
                visualMultiplier *= 1.5f;
            else if (recoilPreset == RecoilFeelPreset.Arcade)
                visualMultiplier *= 0.8f;

            Vector3 weaponRecoil = new Vector3(
                -verticalRecoil * visualMultiplier,
                randomHorizontal * visualMultiplier * 0.5f,
                randomHorizontal * visualMultiplier * 0.3f
            );

            weaponRotationRecoil += weaponRecoil;
            weaponPositionRecoil += new Vector3(0, 0, -weaponPositionKickback * recoilMod);

            // Apply to dual recoil points if enabled
            ApplyDualPointRecoil(weaponRecoil);
        }
    }

    // ========================
    // WEAPON SWAY SYSTEM
    // ========================

    void UpdateWeaponSway()
    {
        if (weaponPivot == null) return;

        swayPosition = Vector3.zero;
        swayRotation = Vector3.zero;

        CalculateMouseSway();
        CalculateMovementBob();
        CalculateWeaponTilt();
        CalculateBreathing();
        CalculateLandingImpact();
        CalculateSprintPosition();

        Vector3 finalPosition = currentWeaponPosition + swayPosition + weaponPositionRecoil;
        Quaternion recoilRotation = Quaternion.Euler(weaponRotationRecoil);
        Quaternion swayRotationQuat = Quaternion.Euler(swayRotation);
        Quaternion finalRotation = originalWeaponRotation * recoilRotation * swayRotationQuat;

        weaponPivot.localPosition = finalPosition;

        // Check if dual point recoil is ACTUALLY set up (not just enabled)
        bool dualPointsActive = useDualPointRecoil && recoilPointTransforms.Count > 0 && currentWeaponData != null && currentWeaponData.recoilPoints.Count > 0;

        if (!dualPointsActive)
        {
            // Standard recoil: apply full rotation to weapon pivot
            weaponPivot.localRotation = finalRotation;
        }
        else
        {
            // Dual point recoil: weapon pivot only gets sway, points handle recoil
            weaponPivot.localRotation = originalWeaponRotation * swayRotationQuat;
            // Note: Individual points get recoil in ApplyDualPointRecoil()
        }
    }

    void CalculateMouseSway()
    {
        float swayMod = isAiming ? adsSwayMultiplier : 1f;

        // Apply breath hold stabilization
        if (isHoldingBreath)
        {
            swayMod *= (1f - breathStabilizationAmount);
        }

        // Apply exhaustion penalty
        if (isExhausted)
        {
            float exhaustionProgress = exhaustedTimer / exhaustedSwayDuration;
            float exhaustionCurve = 1f - exhaustionProgress; // Decreases over time
            swayMod *= (1f + (exhaustedSwayMultiplier - 1f) * exhaustionCurve);
        }

        float mouseX = Input.GetAxis("Mouse X") * swayAmount * swayMod;
        float mouseY = Input.GetAxis("Mouse Y") * swayAmount * swayMod;

        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

        Vector3 targetSwayPosition = new Vector3(-mouseX, -mouseY, 0);
        Vector3 targetSwayRotation = new Vector3(mouseY * 100f, -mouseX * 100f, 0);

        swayPosition += Vector3.Lerp(swayPosition, targetSwayPosition, Time.deltaTime * swaySmoothness);
        swayRotation += Vector3.Lerp(swayRotation, targetSwayRotation, Time.deltaTime * swaySmoothness);
    }

    void CalculateMovementBob()
    {
        if (playerMovement == null || isAiming) return;

        float currentSpeed = playerMovement.GetCurrentSpeed();
        bool isGrounded = playerMovement.IsGrounded();
        bool isRolling = playerMovement.IsRolling();

        // Check if weapon is reloading
        bool isReloading = false;
        if (weaponManager != null)
        {
            WeaponController currentWeapon = weaponManager.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                isReloading = currentWeapon.IsReloading();
            }
        }

        // Detect when roll just ended
        if (wasRolling && !isRolling)
        {
            bobTimer = 0f; // Reset timer when exiting roll
        }

        // Don't bob while rolling OR reloading
        if (isRolling || isReloading)
        {
            bobTimer = 0f;
            wasRolling = isRolling; // Only track rolling state
            return;
        }

        wasRolling = false;

        if (currentSpeed > 0.1f && isGrounded)
        {
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float bobAmount = isRunning ? runBobAmount : walkBobAmount;
            float swayAmt = isRunning ? runSwayAmount : walkSwayAmount;

            // Apply breath hold reduction
            if (isHoldingBreath)
            {
                bobAmount *= (1f - breathBobReduction);
                swayAmt *= (1f - breathBobReduction);
            }

            // Clamp speed to prevent crazy bob rates
            float clampedSpeed = Mathf.Clamp(currentSpeed, 0f, 10f);
            bobTimer += Time.deltaTime * bobSpeed * (clampedSpeed / 5f);

            float bobY = bobCurve.Evaluate(Mathf.PingPong(bobTimer, 1f)) * bobAmount;
            float bobX = Mathf.Sin(bobTimer * 0.5f) * swayAmt;

            swayPosition += new Vector3(bobX, bobY, 0);
        }
        else
        {
            bobTimer = 0f;
        }
    }

    void CalculateWeaponTilt()
    {
        if (playerMovement == null || isAiming) return;

        float currentSpeed = playerMovement.GetCurrentSpeed();
        float moveX = Input.GetAxis("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float targetTilt = 0f;
        if (currentSpeed > 0.1f)
        {
            float tiltMultiplier = isRunning ? runTiltMultiplier : 1f;
            targetTilt = -moveX * tiltAmount * tiltMultiplier;
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothness);
        swayRotation.z += currentTilt;
    }

    void CalculateBreathing()
    {
        if (!enableBreathing) return;

        // No breathing sway when holding breath
        if (isHoldingBreath)
        {
            breathingTimer = 0f;
            return;
        }

        float breathMod = isAiming ? 0.5f : 1f;
        float currentSpeed = playerMovement != null ? playerMovement.GetCurrentSpeed() : 0f;

        // Increase breathing when exhausted
        if (isExhausted)
        {
            breathMod *= exhaustedSwayMultiplier * 0.5f; // Heavy breathing
        }

        if (currentSpeed < 0.1f)
        {
            breathingTimer += Time.deltaTime * breathingSpeed;

            float breathY = Mathf.Sin(breathingTimer) * breathingAmplitude * breathMod;
            float breathX = Mathf.Sin(breathingTimer * 0.5f) * breathingAmplitude * 0.5f * breathMod;

            swayPosition += new Vector3(breathX, breathY, 0);
        }
    }

    void CalculateLandingImpact()
    {
        if (playerMovement == null) return;

        bool isGrounded = playerMovement.IsGrounded();

        if (!wasGrounded && isGrounded)
        {
            landingDip = landingImpact;
        }

        if (landingDip > 0)
        {
            landingDip = Mathf.Lerp(landingDip, 0, Time.deltaTime * landingRecoverySpeed);
            swayPosition.y -= landingDip;
        }

        wasGrounded = isGrounded;
    }

    void CalculateSprintPosition()
    {
        if (isAiming) return;

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = playerMovement != null ? playerMovement.GetCurrentSpeed() : 0f;

        bool shouldSprint = isRunning && currentSpeed > 6f;
        float targetSprintProgress = shouldSprint ? 1f : 0f;
        sprintProgress = Mathf.Lerp(sprintProgress, targetSprintProgress, Time.deltaTime * sprintTransitionSpeed);

        swayPosition.z += sprintForwardAmount * sprintProgress;
        swayPosition.y -= sprintDownAmount * sprintProgress;
    }

    // ========================
    // HELD ITEM SWAY (Future Implementation)
    // ========================

    void UpdateHeldItemSway()
    {
        if (!enableItemSway || heldItemTransform == null) return;
    }

    // ========================
    // PUBLIC METHODS
    // ========================

    public void SetWeaponPivot(Transform pivot)
    {
        weaponPivot = pivot;
        if (pivot != null)
        {
            originalWeaponPosition = pivot.localPosition;
            originalWeaponRotation = pivot.localRotation;
            currentWeaponPosition = originalWeaponPosition;
        }
        ResetWeaponSway();
    }

    public void SetHeldItem(Transform item)
    {
        heldItemTransform = item;
        if (item != null)
        {
            originalItemPosition = item.localPosition;
            originalItemRotation = item.localRotation;
        }
    }

    public void ResetWeaponSway()
    {
        swayPosition = Vector3.zero;
        swayRotation = Vector3.zero;
        weaponRotationRecoil = Vector3.zero;
        weaponPositionRecoil = Vector3.zero;
        currentTilt = 0f;
        landingDip = 0f;
        bobTimer = 0f;
        breathingTimer = 0f;
        sprintProgress = 0f;
    }

    public void SetRecoilPreset(RecoilFeelPreset preset)
    {
        recoilPreset = preset;
    }

    public WeaponData GetWeaponData()
    {
        return currentWeaponData;
    }

    // ========================
    // GIZMOS (Editor Only)
    // ========================

    void OnDrawGizmos()
    {
        if (!showRecoilPointGizmos || !useDualPointRecoil || currentWeaponData == null)
            return;

        if (weaponPivot == null)
            return;

        // Draw recoil points
        if (currentWeaponData.recoilPoints != null)
        {
            foreach (RecoilPoint point in currentWeaponData.recoilPoints)
            {
                Vector3 worldPos = weaponPivot.TransformPoint(point.positionOffset);
                Gizmos.color = point.gizmoColor;
                Gizmos.DrawWireSphere(worldPos, gizmoSize);
                Gizmos.DrawLine(weaponPivot.position, worldPos);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(worldPos, point.pointName);
#endif
            }
        }
    }
}

public enum RecoilFeelPreset
{
    Realistic,
    Balanced,
    Arcade,
    Heavy,
    Custom
}
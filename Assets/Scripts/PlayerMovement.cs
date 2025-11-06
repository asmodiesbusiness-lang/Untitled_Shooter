using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("=== MOVEMENT TOGGLES ===")]
    [SerializeField] private bool enableWalking = true;
    [SerializeField] private bool enableSprinting = true;
    [SerializeField] private bool enableJumping = true;
    [SerializeField] private bool enableCrouching = true;
    [SerializeField] private bool enableSliding = true;
    [SerializeField] private bool enableProne = true;
    [SerializeField] private bool enableDiving = true;

    [Header("=== MOVEMENT ===")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float proneSpeed = 1.5f;
    private float currentSpeed;

    [Header("=== CONTROLLER ===")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private float gravity = -20f;
    private Vector3 velocity;

    [Header("=== STANCE ===")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private float proneHeight = 0.6f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    private enum Stance { Standing, Crouching, Prone }
    private Stance currentStance = Stance.Standing;
    private Stance targetStance = Stance.Standing;

    [Header("=== INPUT ===")]
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode slideKey = KeyCode.V;
    [SerializeField] private KeyCode diveKey = KeyCode.C;

    [Header("=== GROUND CHECK ===")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Transform groundCheck;
    private bool isGrounded;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, -0.9f, 0f);
    [SerializeField] private float groundCheckRadius = 0.4f;

    [Header("=== JUMP ===")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float airControlMultiplier = 0.5f;
    private float nextJumpTime = 0f;
    private float lastGroundedTime = 0f;

    [Header("=== PRONE ===")]
    [SerializeField] private float proneHoldTime = 1f;
    private float crouchHoldTimer = 0f;

    [Header("=== SLIDE ===")]
    [SerializeField] private float slideSpeed = 10f;
    [SerializeField] private float slideDuration = 1f;
    [SerializeField] private float slideCooldown = 2f;
    [SerializeField] private AnimationCurve slideHeightCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDirection;
    private float lastSlideTime;

    [Header("=== CONTINUOUS WEAPON SNAP ===")]
    [Tooltip("Enable continuous weapon snap system")]
    [SerializeField] private bool enableWeaponSnap = true;

    [Tooltip("Only snap Y rotation (yaw) - allows free pitch/roll from breathing/bob")]
    [SerializeField] private bool snapYawOnly = true;

    [Tooltip("Acceptable rotation deviation (degrees) - weapon snaps if outside this")]
    [SerializeField] private float rotationTolerance = 15f;

    [Tooltip("Acceptable position deviation (meters) - weapon snaps if outside this")]
    [SerializeField] private float positionTolerance = 0.15f;

    [Tooltip("Snap speed (0 = instant, higher = smoother but slower)")]
    [SerializeField] private float snapSpeed = 0f;

    [Tooltip("Pause snap during these states")]
    [SerializeField] private bool pauseSnapDuringReload = true;
    [SerializeField] private bool pauseSnapDuringSwitch = true;
    [SerializeField] private bool pauseSnapDuringDive = true;
    [SerializeField] private bool pauseSnapWhileProne = true;

    [SerializeField] private Transform weaponHolder;

    private Vector3 weaponOriginalPosition;
    private Quaternion weaponOriginalRotation;
    private bool weaponTransformStored = false;

    [Header("=== DIVE ROTATION ===")]
    [SerializeField] private float diveRollAngle = 360f;
    [SerializeField] private float diveRotationSpeed = 8f;
    [SerializeField] private Vector3 diveRollAxis = new Vector3(1, 0, 0);
    [SerializeField] private bool keepWeaponUpright = true;
    [Range(0f, 1f)]
    [SerializeField] private float weaponCameraFollowStrength = 0.7f;
    [SerializeField] private float weaponFollowSpeed = 8f;

    [Header("=== DIVE SETTINGS ===")]
    [SerializeField] private float diveVerticalForce = 3f;
    [SerializeField] private float diveHorizontalForce = 8f;
    [SerializeField] private float diveDuration = 1.5f;
    [SerializeField] private float diveMinAirTime = 0.3f;
    [SerializeField] private float diveGroundDetectionDistance = 0.2f;
    [SerializeField] private float diveCooldown = 2f;
    [SerializeField] private AnimationCurve diveJumpCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private bool reduceColliderDuringDive = true;
    [SerializeField] private float colliderHeightDuringDive = 0.8f;

    private bool isDiving;
    private float diveTimer;
    private Vector3 diveDirection;
    private float lastDiveTime;
    private Quaternion diveStartRotation;
    private float originalControllerHeight;
    private float distanceToGround;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGroundGizmos = true;
    [SerializeField] private bool showDiveDebug = true;
    [SerializeField] private bool logStateChanges = false;

    private Camera playerCamera;
    private ScopeManager scopeManager;
    private WeaponManager weaponManager;
    private WeaponController currentWeapon;

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        standingHeight = controller.height;
        originalControllerHeight = standingHeight;
        currentSpeed = walkSpeed;
        controller.center = Vector3.zero;
        controller.minMoveDistance = 0.001f;
        controller.skinWidth = 0.08f;

        if (groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            groundCheck = go.transform;
        }
        groundCheck.localPosition = groundCheckOffset;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        scopeManager = GetComponent<ScopeManager>();
        if (scopeManager == null)
            scopeManager = GetComponentInChildren<ScopeManager>();
        if (scopeManager == null)
            scopeManager = FindFirstObjectByType<ScopeManager>();

        weaponManager = GetComponentInChildren<WeaponManager>();
        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();

        if (weaponHolder != null)
        {
            StoreWeaponTransform();
        }
    }

    void StoreWeaponTransform()
    {
        if (weaponHolder != null)
        {
            weaponOriginalPosition = weaponHolder.localPosition;
            weaponOriginalRotation = weaponHolder.localRotation;
            weaponTransformStored = true;
        }
    }

    void Update()
    {
        groundCheck.localPosition = groundCheckOffset;

        if (weaponManager != null)
            currentWeapon = weaponManager.GetCurrentWeapon();

        CheckGround();
        HandleContinuousWeaponSnap();
        HandleStance();
        HandleDive();
        HandleSlide();
        HandleMovement();
        HandleJump();
        HandleWeaponFollowCamera();
        ApplyGravity();
    }

    void OnGUI()
    {
        if (showDebugInfo)
        {
            DrawDebugOverlay();
        }
    }

    void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f, groundMask))
        {
            distanceToGround = hit.distance;
        }
        else
        {
            distanceToGround = 999f;
        }

        if (isGrounded)
        {
            lastGroundedTime = Time.time;

            if (velocity.y < 0)
                velocity.y = -2f;
        }
    }

    void HandleContinuousWeaponSnap()
    {
        if (!enableWeaponSnap || weaponHolder == null || !weaponTransformStored)
            return;

        bool shouldPauseSnap = false;

        if (pauseSnapDuringDive && isDiving)
            shouldPauseSnap = true;

        if (pauseSnapDuringReload && currentWeapon != null && currentWeapon.IsReloading())
            shouldPauseSnap = true;

        if (pauseSnapWhileProne && currentStance == Stance.Prone)
            shouldPauseSnap = true;

        if (shouldPauseSnap)
            return;

        Vector3 currentEuler = weaponHolder.localRotation.eulerAngles;
        Vector3 targetEuler = weaponOriginalRotation.eulerAngles;

        bool rotationOutOfBounds = false;

        if (snapYawOnly)
        {
            float yDiff = Mathf.DeltaAngle(currentEuler.y, targetEuler.y);
            rotationOutOfBounds = Mathf.Abs(yDiff) > rotationTolerance;
        }
        else
        {
            float xDiff = Mathf.DeltaAngle(currentEuler.x, targetEuler.x);
            float yDiff = Mathf.DeltaAngle(currentEuler.y, targetEuler.y);
            float zDiff = Mathf.DeltaAngle(currentEuler.z, targetEuler.z);

            rotationOutOfBounds = Mathf.Abs(xDiff) > rotationTolerance ||
                                  Mathf.Abs(yDiff) > rotationTolerance ||
                                  Mathf.Abs(zDiff) > rotationTolerance;
        }

        float positionDistance = Vector3.Distance(weaponHolder.localPosition, weaponOriginalPosition);
        bool positionOutOfBounds = positionDistance > positionTolerance;

        if (rotationOutOfBounds)
        {
            if (snapSpeed <= 0.01f)
            {
                if (snapYawOnly)
                {
                    Vector3 fixedEuler = weaponHolder.localRotation.eulerAngles;
                    fixedEuler.y = weaponOriginalRotation.eulerAngles.y;
                    weaponHolder.localRotation = Quaternion.Euler(fixedEuler);
                }
                else
                {
                    weaponHolder.localRotation = weaponOriginalRotation;
                }
            }
            else
            {
                weaponHolder.localRotation = Quaternion.Slerp(
                    weaponHolder.localRotation,
                    weaponOriginalRotation,
                    Time.deltaTime * snapSpeed
                );
            }
        }

        if (positionOutOfBounds)
        {
            if (snapSpeed <= 0.01f)
            {
                weaponHolder.localPosition = weaponOriginalPosition;
            }
            else
            {
                weaponHolder.localPosition = Vector3.Lerp(
                    weaponHolder.localPosition,
                    weaponOriginalPosition,
                    Time.deltaTime * snapSpeed
                );
            }
        }
    }

    void HandleStance()
    {
        if (isDiving || isSliding)
            return;

        if (enableCrouching && Input.GetKey(crouchKey))
        {
            crouchHoldTimer += Time.deltaTime;

            if (targetStance == Stance.Standing)
                targetStance = Stance.Crouching;

            if (enableProne && crouchHoldTimer >= proneHoldTime && targetStance != Stance.Prone && isGrounded)
                targetStance = Stance.Prone;
        }
        else
        {
            crouchHoldTimer = 0f;

            if (targetStance != Stance.Standing && currentStance != Stance.Prone)
                targetStance = Stance.Standing;
        }

        float targetHeight = standingHeight;
        switch (targetStance)
        {
            case Stance.Crouching:
                targetHeight = crouchHeight;
                currentSpeed = crouchSpeed;
                break;
            case Stance.Prone:
                targetHeight = proneHeight;
                currentSpeed = proneSpeed;
                break;
            case Stance.Standing:
                currentSpeed = walkSpeed;
                break;
        }

        float prevHeight = controller.height;
        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.center = Vector3.zero;

        float heightDiff = prevHeight - controller.height;
        if (heightDiff > 0.01f)
            transform.position += Vector3.up * heightDiff;

        if (Mathf.Abs(controller.height - targetHeight) < 0.05f)
            currentStance = targetStance;
    }

    void HandleDive()
    {
        if (!enableDiving) return;

        if (isDiving)
        {
            diveTimer += Time.deltaTime;
            float t = diveTimer / diveDuration;
            float curve = diveJumpCurve.Evaluate(t);

            controller.Move(diveDirection * diveHorizontalForce * curve * Time.deltaTime);

            Transform rotateTarget = (keepWeaponUpright && weaponHolder != null) ? transform : (weaponHolder != null ? weaponHolder : transform);

            float rollProgress = Mathf.Clamp01(t);
            float currentAngle = diveRollAngle * rollProgress;
            Vector3 rollEuler = diveRollAxis.normalized * currentAngle;
            Quaternion targetRoll = diveStartRotation * Quaternion.Euler(rollEuler);

            if (!keepWeaponUpright || rotateTarget == transform)
            {
                rotateTarget.rotation = Quaternion.Slerp(rotateTarget.rotation, targetRoll, Time.deltaTime * diveRotationSpeed);
            }

            bool enoughAirTime = diveTimer >= diveMinAirTime;
            bool closeToGround = distanceToGround <= diveGroundDetectionDistance;
            bool diveTimeExpired = diveTimer >= diveDuration;

            bool shouldEndDive = (enoughAirTime && (isGrounded || closeToGround)) || diveTimeExpired;

            if (shouldEndDive)
            {
                EndDive();
            }

            return;
        }

        if (Input.GetKeyDown(diveKey) && isGrounded && Time.time - lastDiveTime >= diveCooldown)
        {
            Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            dir = transform.TransformDirection(dir);

            if (dir.magnitude >= 0.1f)
            {
                StartDive(dir.normalized);
            }
        }
    }

    void StartDive(Vector3 direction)
    {
        diveDirection = direction;
        isDiving = true;
        diveTimer = 0f;
        lastDiveTime = Time.time;
        velocity.y = diveVerticalForce;

        if (keepWeaponUpright && weaponHolder != null)
        {
            diveStartRotation = transform.rotation;
        }
        else if (weaponHolder != null)
        {
            diveStartRotation = weaponHolder.rotation;
        }
        else
        {
            diveStartRotation = transform.rotation;
        }

        if (reduceColliderDuringDive)
        {
            originalControllerHeight = controller.height;
            controller.height = colliderHeightDuringDive;
            controller.center = Vector3.zero;
        }
    }

    void EndDive()
    {
        isDiving = false;

        controller.height = proneHeight;
        controller.center = Vector3.zero;
        targetStance = Stance.Prone;
        currentStance = Stance.Prone;
    }

    void HandleWeaponFollowCamera()
    {
        if (weaponHolder == null || playerCamera == null || isDiving) return;

        float cameraYaw = playerCamera.transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, cameraYaw, 0);
        Quaternion currentRotation = weaponHolder.localRotation;
        Quaternion blendedRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, weaponCameraFollowStrength);

        weaponHolder.localRotation = Quaternion.Slerp(currentRotation, blendedRotation, Time.deltaTime * weaponFollowSpeed);
    }

    void HandleSlide()
    {
        if (!enableSliding) return;

        if (isSliding)
        {
            slideTimer += Time.deltaTime;
            controller.Move(slideDirection * slideSpeed * Time.deltaTime);

            float t = slideTimer / slideDuration;
            float curve = slideHeightCurve.Evaluate(t);
            float heightMult = 1f - (curve * 0.6f);

            float prevHeight = controller.height;
            controller.height = Mathf.Lerp(controller.height, standingHeight * heightMult, Time.deltaTime * 15f);
            controller.center = Vector3.zero;

            float heightDiff = prevHeight - controller.height;
            if (heightDiff > 0.01f)
                transform.position += Vector3.up * heightDiff;

            if (slideTimer >= slideDuration)
            {
                isSliding = false;
                slideTimer = 0f;
            }
            return;
        }

        if (Input.GetKeyDown(slideKey) && isGrounded && Time.time - lastSlideTime >= slideCooldown)
        {
            Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            dir = transform.TransformDirection(dir);

            if (dir.magnitude < 0.1f)
                dir = transform.forward;

            slideDirection = dir.normalized;
            isSliding = true;
            lastSlideTime = Time.time;
            slideTimer = 0f;
        }
    }

    void HandleMovement()
    {
        if (!enableWalking || isSliding || isDiving)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool sprint = enableSprinting && Input.GetKey(KeyCode.LeftShift) && v > 0 && currentStance == Stance.Standing;

        float speed = sprint ? sprintSpeed : currentSpeed;
        Vector3 move = transform.right * h + transform.forward * v;
        float mult = isGrounded ? 1f : airControlMultiplier;

        controller.Move(move * speed * mult * Time.deltaTime);
    }

    void HandleJump()
    {
        if (!enableJumping) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool coyote = Time.time - lastGroundedTime < coyoteTime;
            bool canJump = (isGrounded || coyote) && !isSliding && !isDiving && currentStance == Stance.Standing && Time.time >= nextJumpTime;

            if (canJump)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                nextJumpTime = Time.time + jumpCooldown;
            }
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
            velocity.y = Mathf.Max(velocity.y, -20f);
        }

        controller.Move(velocity * Time.deltaTime);
    }

    void DrawDebugOverlay()
    {
        GUILayout.BeginArea(new Rect(10, 10, 450, 550));
        GUILayout.Box("=== PLAYER MOVEMENT DEBUG ===");

        GUILayout.Label("Stance: " + currentStance + " -> " + targetStance);
        GUILayout.Label("Grounded: " + (isGrounded ? "YES" : "NO") + " (Dist: " + distanceToGround.ToString("F2") + "m)");
        GUILayout.Label("Velocity: " + velocity.magnitude.ToString("F2") + " m/s");
        GUILayout.Label("Speed: " + currentSpeed.ToString("F1") + " m/s");

        if (scopeManager != null)
        {
            bool isADS = scopeManager.IsADS();
            GUILayout.Label("ADS: " + (isADS ? "YES" : "NO"));
        }
        else
        {
            GUILayout.Label("ADS: SCOPE MANAGER NOT FOUND");
        }

        if (enableWeaponSnap && weaponHolder != null && weaponTransformStored)
        {
            Vector3 currentEuler = weaponHolder.localRotation.eulerAngles;
            Vector3 targetEuler = weaponOriginalRotation.eulerAngles;

            float xDiff = Mathf.Abs(Mathf.DeltaAngle(currentEuler.x, targetEuler.x));
            float yDiff = Mathf.Abs(Mathf.DeltaAngle(currentEuler.y, targetEuler.y));
            float zDiff = Mathf.Abs(Mathf.DeltaAngle(currentEuler.z, targetEuler.z));

            float posDist = Vector3.Distance(weaponHolder.localPosition, weaponOriginalPosition);

            bool rotOK = (snapYawOnly && yDiff < rotationTolerance) ||
                         (!snapYawOnly && xDiff < rotationTolerance && yDiff < rotationTolerance && zDiff < rotationTolerance);
            bool posOK = posDist < positionTolerance;

            string rotStatus = rotOK ? "OK" : "OUT (" + (snapYawOnly ? yDiff.ToString("F1") : Mathf.Max(xDiff, yDiff, zDiff).ToString("F1")) + "deg)";
            string posStatus = posOK ? "OK" : "OUT (" + posDist.ToString("F3") + "m)";

            GUILayout.Label("Weapon Snap: Rot=" + rotStatus + " Pos=" + posStatus);
            GUILayout.Label("  Mode: " + (snapYawOnly ? "YAW ONLY" : "ALL AXES"));
            GUILayout.Label("  Tolerance: Rot=" + rotationTolerance + "deg Pos=" + positionTolerance.ToString("F3") + "m");
            GUILayout.Label("  Speed: " + (snapSpeed <= 0.01f ? "INSTANT" : snapSpeed.ToString("F1")));

            if (pauseSnapDuringDive && isDiving)
                GUILayout.Label("  Snap PAUSED: Diving");
            else if (pauseSnapDuringReload && currentWeapon != null && currentWeapon.IsReloading())
                GUILayout.Label("  Snap PAUSED: Reloading");
            else if (pauseSnapWhileProne && currentStance == Stance.Prone)
                GUILayout.Label("  Snap PAUSED: Prone");
            else
                GUILayout.Label("  Snap ACTIVE");
        }

        if (isDiving)
        {
            GUILayout.Label("=== DIVING ===");
            GUILayout.Label("Timer: " + diveTimer.ToString("F2") + "s / " + diveDuration.ToString("F2") + "s");
            GUILayout.Label("Progress: " + ((diveTimer / diveDuration) * 100).ToString("F0") + "%");
            GUILayout.Label("Min Air Time: " + (diveTimer >= diveMinAirTime ? "MET" : "WAITING"));
            GUILayout.Label("Ground Dist: " + distanceToGround.ToString("F2") + "m");
        }

        if (weaponHolder != null)
        {
            GUILayout.Label("Weapon Follow: " + weaponCameraFollowStrength.ToString("F2") + " @ " + weaponFollowSpeed.ToString("F1"));
        }

        GUILayout.EndArea();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGroundGizmos) return;

        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distanceToGround);
        }

        if (showDiveDebug && isDiving)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, diveDirection * 2f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * diveGroundDetectionDistance, 0.1f);
        }
    }

    public bool IsSliding() => isSliding;
    public bool IsDiving() => isDiving;
    public bool IsRolling() => isSliding;
    public bool IsProne() => currentStance == Stance.Prone;
    public bool IsGrounded() => isGrounded;
    public float GetCurrentSpeed() => currentSpeed;
    public bool isCrouching => currentStance == Stance.Crouching;

    public void OnWeaponChanged()
    {
        if (weaponHolder != null)
        {
            Invoke(nameof(StoreWeaponTransform), 0.1f);
        }
    }
}
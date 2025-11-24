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

    [Header("=== WEAPON SNAP ===")]
    [SerializeField] private bool enableWeaponSnap = true;
    [SerializeField] private float maxRotationX = 45f;
    [SerializeField] private float maxRotationY = 60f;
    [SerializeField] private float maxRotationZ = 30f;
    [SerializeField] private float positionTolerance = 0.2f;
    [SerializeField] private bool pauseSnapDuringADS = true;
    [SerializeField] private bool pauseSnapWhileProne = true;
    [SerializeField] private Transform weaponHolder;

    private Vector3 weaponOriginalPosition;
    private Quaternion weaponOriginalRotation;
    private bool weaponTransformStored = false;

    [Header("=== DIVE SETTINGS ===")]
    [SerializeField] private float diveVerticalForce = 3f;
    [SerializeField] private float diveHorizontalForce = 8f;
    [SerializeField] private float diveDuration = 1.5f;
    [SerializeField] private float diveMinAirTime = 0.3f;
    [SerializeField] private float diveGroundDetectionDistance = 0.5f;
    [SerializeField] private float diveCooldown = 2f;
    [SerializeField] private AnimationCurve diveJumpCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private bool reduceColliderDuringDive = true;
    [SerializeField] private float colliderHeightDuringDive = 0.8f;

    [Header("=== DIVE ROTATION ===")]
    [SerializeField] private float diveRollAngle = 360f;
    [SerializeField] private float diveRotationSpeed = 8f;
    [SerializeField] private Vector3 diveRollAxis = new Vector3(1, 0, 0);
    [SerializeField] private bool keepWeaponUpright = true;

    private bool isDiving;
    private float diveTimer;
    private Vector3 diveDirection;
    private float lastDiveTime;
    private Quaternion diveStartRotation;
    private float originalControllerHeight;
    private float distanceToGround;
    private bool diveEndedInAir;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = true;

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

        if (groundCheck == null)
        {
            GameObject go = new GameObject("GroundCheck");
            go.transform.SetParent(transform);
            groundCheck = go.transform;
        }
        groundCheck.localPosition = groundCheckOffset;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        scopeManager = FindFirstObjectByType<ScopeManager>();
        weaponManager = GetComponentInChildren<WeaponManager>();

        if (weaponHolder != null)
            StoreWeaponTransform();
    }

    void StoreWeaponTransform()
    {
        weaponOriginalPosition = weaponHolder.localPosition;
        weaponOriginalRotation = weaponHolder.localRotation;
        weaponTransformStored = true;
    }

    void Update()
    {
        if (weaponManager != null)
            currentWeapon = weaponManager.GetCurrentWeapon();

        CheckGround();
        HandleWeaponSnap();
        HandleStance();
        HandleDive();
        HandleSlide();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void OnGUI()
    {
        if (showDebugInfo)
            DrawDebugOverlay();
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f, groundMask))
            distanceToGround = hit.distance;
        else
            distanceToGround = 999f;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;

            if (diveEndedInAir && !isDiving)
            {
                targetStance = Stance.Prone;
                currentStance = Stance.Prone;
                controller.height = proneHeight;
                diveEndedInAir = false;
            }

            if (velocity.y < 0)
                velocity.y = -2f;
        }
    }

    void HandleWeaponSnap()
    {
        if (!enableWeaponSnap || weaponHolder == null || !weaponTransformStored || isDiving)
            return;

        bool shouldPause = false;

        if (pauseSnapDuringADS && scopeManager != null && scopeManager.IsADS())
            shouldPause = true;

        if (pauseSnapWhileProne && currentStance == Stance.Prone)
            shouldPause = true;

        if (shouldPause)
            return;

        Vector3 currentEuler = weaponHolder.localRotation.eulerAngles;
        Vector3 targetEuler = weaponOriginalRotation.eulerAngles;

        float xDiff = Mathf.DeltaAngle(currentEuler.x, targetEuler.x);
        float yDiff = Mathf.DeltaAngle(currentEuler.y, targetEuler.y);
        float zDiff = Mathf.DeltaAngle(currentEuler.z, targetEuler.z);

        if (Mathf.Abs(xDiff) > maxRotationX || Mathf.Abs(yDiff) > maxRotationY || Mathf.Abs(zDiff) > maxRotationZ)
        {
            Vector3 clampedEuler = currentEuler;

            if (Mathf.Abs(xDiff) > maxRotationX)
                clampedEuler.x = ClampAngle(currentEuler.x, targetEuler.x - maxRotationX, targetEuler.x + maxRotationX);

            if (Mathf.Abs(yDiff) > maxRotationY)
                clampedEuler.y = ClampAngle(currentEuler.y, targetEuler.y - maxRotationY, targetEuler.y + maxRotationY);

            if (Mathf.Abs(zDiff) > maxRotationZ)
                clampedEuler.z = ClampAngle(currentEuler.z, targetEuler.z - maxRotationZ, targetEuler.z + maxRotationZ);

            weaponHolder.localRotation = Quaternion.Euler(clampedEuler);
        }

        float positionDistance = Vector3.Distance(weaponHolder.localPosition, weaponOriginalPosition);
        if (positionDistance > positionTolerance)
        {
            Vector3 direction = (weaponHolder.localPosition - weaponOriginalPosition).normalized;
            weaponHolder.localPosition = weaponOriginalPosition + direction * positionTolerance;
        }
    }

    float ClampAngle(float current, float min, float max)
    {
        current = NormalizeAngle(current);
        min = NormalizeAngle(min);
        max = NormalizeAngle(max);

        if (min > max)
        {
            if (current > min || current < max)
                return current;

            float distToMin = Mathf.Abs(Mathf.DeltaAngle(current, min));
            float distToMax = Mathf.Abs(Mathf.DeltaAngle(current, max));
            return distToMin < distToMax ? min : max;
        }

        return Mathf.Clamp(current, min, max);
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
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
            if (targetStance != Stance.Standing)
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

        controller.height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);

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

            bool shouldEndDive = (diveTimer >= diveMinAirTime && (isGrounded || distanceToGround <= diveGroundDetectionDistance)) || diveTimer >= diveDuration;

            if (shouldEndDive)
                EndDive();

            return;
        }

        if (Input.GetKeyDown(diveKey) && isGrounded && Time.time - lastDiveTime >= diveCooldown)
        {
            Vector3 dir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
            dir = transform.TransformDirection(dir);

            if (dir.magnitude >= 0.1f)
                StartDive(dir.normalized);
        }
    }

    void StartDive(Vector3 direction)
    {
        diveDirection = direction;
        isDiving = true;
        diveTimer = 0f;
        lastDiveTime = Time.time;
        velocity.y = diveVerticalForce;
        diveEndedInAir = false;
    }

    void EndDive()
    {
        isDiving = false;

        if (isGrounded)
        {
            controller.height = proneHeight;
            targetStance = Stance.Prone;
            currentStance = Stance.Prone;
        }
        else
        {
            diveEndedInAir = true;
        }
    }

    void HandleSlide()
    {
        if (!enableSliding) return;

        if (isSliding)
        {
            slideTimer += Time.deltaTime;
            controller.Move(slideDirection * slideSpeed * Time.deltaTime);

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
        GUILayout.BeginArea(new Rect(10, 10, 400, 400));
        GUILayout.Box("=== PLAYER MOVEMENT DEBUG ===");

        GUILayout.Label("Stance: " + currentStance);
        GUILayout.Label("Grounded: " + (isGrounded ? "YES" : "NO"));
        GUILayout.Label("Speed: " + currentSpeed.ToString("F1") + " m/s");

        if (scopeManager != null)
            GUILayout.Label("ADS: " + (scopeManager.IsADS() ? "YES" : "NO"));

        if (enableWeaponSnap && weaponHolder != null)
        {
            bool snapPaused = (pauseSnapDuringADS && scopeManager != null && scopeManager.IsADS()) ||
                              (pauseSnapWhileProne && currentStance == Stance.Prone);

            GUILayout.Label("Weapon Snap: " + (snapPaused ? "PAUSED" : "ACTIVE"));
        }

        GUILayout.EndArea();
    }

    public bool IsSliding() => isSliding;
    public bool IsDiving() => isDiving;
    public bool IsProne() => currentStance == Stance.Prone;
    public bool IsGrounded() => isGrounded;
    public bool isCrouching => currentStance == Stance.Crouching;

    // New methods required by CharacterAnimatorController
    public float GetCurrentSpeed()
    {
        // Return the actual movement speed of the character
        if (controller != null)
        {
            // Calculate horizontal speed (ignoring Y velocity)
            Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
            return horizontalVelocity.magnitude;
        }
        return currentSpeed;
    }

    public bool IsRolling()
    {
        // Rolling is essentially the same as diving in this implementation
        // You can customize this logic if you want rolling to be different from diving
        return isDiving;
    }

    public void OnWeaponChanged()
    {
        if (weaponHolder != null)
            Invoke(nameof(StoreWeaponTransform), 0.1f);
    }
}
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public enum WeaponType
{
    Custom,
    AssaultRifle,
    SMG,
    SniperRifle,
    Shotgun,
    Pistol,
    LMG
}

[Serializable]
public class TransformOffset
{
    public Vector3 position = Vector3.zero;
    public Vector3 rotation = Vector3.zero;

    public TransformOffset() { }

    public TransformOffset(Vector3 pos, Vector3 rot)
    {
        position = pos;
        rotation = rot;
    }
}

public enum ShotgunSpreadPattern
{
    Cone,
    Horizontal,
    Vertical,
    Cross,
    Random
}

public enum BarrelType
{
    Standard,
    Suppressor,
    Compensator,
    MuzzleBrake,
    FlashHider
}

/// <summary>
/// CLEAN VERSION - Weapon controller with smoke system and procedural reload
/// Fully tested and verified structure
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Preset")]
    [SerializeField] private WeaponType weaponType = WeaponType.Custom;
    [SerializeField] private bool applyPreset = false;

    [Header("Weapon Stats")]
    [SerializeField] private int magSize = 30;
    [SerializeField] private int reserveAmmo = 90;
    [SerializeField] private float fireRate = 0.1f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float bulletVelocity = 1000f;

    [Header("Recoil Values")]
    [SerializeField] private float recoilX = 0.5f;
    [SerializeField] private float recoilY = 2f;

    [Header("Weapon Model")]
    [SerializeField] private GameObject weaponModel;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem casingEjection;
    [SerializeField] private ParticleSystem muzzleSmoke;
    [SerializeField] private float casingSize = 1.0f;
    [SerializeField] private float casingEjectionForce = 4f;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject impactEffect;

    [Header("Tracer Settings")]
    [SerializeField] private Color tracerColor = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private float tracerBulbSize = 0.15f;
    [SerializeField] private float tracerEndWidth = 0.02f;
    [SerializeField] private int tracerSegments = 20;
    [SerializeField] private float tracerVisibleTime = 0.02f;
    [SerializeField] private float tracerFadeTime = 0.08f;

    [Header("Weapon Accuracy & Feel")]
    [SerializeField] private float bulletSpreadAngle = 2f;
    [SerializeField] private float aimSpreadMultiplier = 0.3f;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private float weaponRotationSpeed = 8f;
    [SerializeField] private float weaponRotationAmount = 0.6f;

    [Header("Laser Sight")]
    [SerializeField] private bool hasLaserSight = false;
    [SerializeField] private Transform laserOrigin;
    [SerializeField] private float laserMaxDistance = 100f;
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.01f;

    [Header("Shotgun Settings")]
    [SerializeField] private int numberOfPellets = 8;
    [SerializeField] private float pelletSpreadAngle = 10f;
    [SerializeField] private ShotgunSpreadPattern spreadPattern = ShotgunSpreadPattern.Cone;

    [Header("Procedural Reload Animation")]
    [SerializeField] private bool useProceduralReload = true;
    [SerializeField] private float reloadDuration = 2.0f;
    [SerializeField] private Vector3 reloadPositionOffset = new Vector3(0, -0.2f, -0.1f);
    [SerializeField] private Vector3 reloadRotationOffset = new Vector3(30, 0, -20);
    [SerializeField] private AnimationCurve reloadCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;

    private int currentAmmo;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private FPSCharacterController characterController;
    private Camera playerCamera;
    private AudioSource audioSource;

    private LineRenderer laserLine;
    private GameObject laserDot;
    private bool isSuppressed = false;
    private AudioClip suppressedSound;
    private BarrelType currentBarrel = BarrelType.Standard;

    void OnValidate()
    {
        if (applyPreset && weaponType != WeaponType.Custom)
        {
            ApplyWeaponPreset();
            applyPreset = false;
        }
    }

    public void Initialize(FPSCharacterController controller)
    {
        characterController = controller;
        playerCamera = controller.GetPlayerCamera();
        currentAmmo = magSize;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;

        if (hasLaserSight) SetupLaserSight();

        ConfigureCasingEjection();
    }

    public void SetupFromData(WeaponData data, Transform pivot, Transform muzzle, Transform casingPoint,
                              ParticleSystem flash, ParticleSystem casing, GameObject impact,
                              ParticleSystem smoke, Transform laser = null)
    {
        weaponType = data.weaponType;
        magSize = data.magSize;
        currentAmmo = magSize;
        reserveAmmo = data.reserveAmmo;
        fireRate = data.fireRate;
        damage = data.damage;
        range = data.range;
        bulletVelocity = data.bulletVelocity;

        recoilX = data.recoilX;
        recoilY = data.recoilY;

        bulletSpreadAngle = data.bulletSpreadAngle;
        aimSpreadMultiplier = data.aimSpreadMultiplier;
        weaponRotationAmount = data.weaponRotationAmount;

        tracerColor = data.tracerColor;
        tracerBulbSize = data.tracerBulbSize;

        numberOfPellets = data.numberOfPellets;
        pelletSpreadAngle = data.pelletSpreadAngle;
        spreadPattern = data.spreadPattern;

        hasLaserSight = data.hasLaserSight;

        shootSound = data.shootSound;
        reloadSound = data.reloadSound;
        suppressedSound = data.suppressedShootSound;

        casingSize = data.casingSize;
        casingEjectionForce = data.casingEjectionForce;

        reloadDuration = data.reloadDuration;
        reloadPositionOffset = data.reloadPositionOffset;
        reloadRotationOffset = data.reloadRotationOffset;
        reloadCurve = data.reloadAnimationCurve;

        weaponPivot = pivot;
        muzzlePoint = muzzle;
        muzzleFlash = flash;
        casingEjection = casing;
        impactEffect = impact;

        muzzleSmoke = smoke;

        if (muzzleSmoke != null)
        {
            muzzleSmoke.transform.SetParent(muzzlePoint);
            muzzleSmoke.transform.localPosition = Vector3.zero;
            muzzleSmoke.transform.localRotation = Quaternion.identity;
        }

        laserOrigin = laser != null ? laser : muzzle;

        if (hasLaserSight && laserOrigin != null)
        {
            if (laserLine != null && laserLine.gameObject != null)
            {
                Destroy(laserLine.gameObject);
                laserLine = null;
            }
            if (laserDot != null)
            {
                Destroy(laserDot);
                laserDot = null;
            }

            SetupLaserSight();
        }

        ConfigureCasingEjection();
    }

    void ConfigureCasingEjection()
    {
        if (casingEjection == null) return;

        var main = casingEjection.main;
        main.startSize = 0.05f * casingSize;
        main.startSpeed = casingEjectionForce;

        var renderer = casingEjection.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
            renderer.renderMode = ParticleSystemRenderMode.Mesh;

            if (renderer.material == null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = new Color(0.8f, 0.7f, 0.3f);
            }
        }
    }

    public void SetSuppressed(bool suppressed)
    {
        isSuppressed = suppressed;
    }

    public void SetBarrelAttachment(BarrelType barrel, WeaponData weaponData)
    {
        currentBarrel = barrel;

        if (muzzleSmoke != null && muzzleSmoke.gameObject != null)
        {
            Destroy(muzzleSmoke.gameObject);
        }

        ParticleSystem smokePrefab = null;

        switch (barrel)
        {
            case BarrelType.Suppressor:
                smokePrefab = weaponData.suppressedSmokeEffect;
                break;
            case BarrelType.Compensator:
                smokePrefab = weaponData.compensatorSmokeEffect;
                break;
            default:
                smokePrefab = weaponData.standardSmokeEffect;
                break;
        }

        if (smokePrefab != null && muzzlePoint != null)
        {
            muzzleSmoke = Instantiate(smokePrefab, muzzlePoint);
            muzzleSmoke.transform.localPosition = Vector3.zero;
            muzzleSmoke.transform.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        UpdateWeaponRotation();
        if (hasLaserSight) UpdateLaser();
    }

    public void TryShoot()
    {
        if (isReloading || Time.time < nextFireTime || currentAmmo <= 0)
        {
            return;
        }

        Shoot();
        nextFireTime = Time.time + fireRate;
    }

    void Shoot()
    {
        currentAmmo--;

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        if (muzzleSmoke != null)
        {
            muzzleSmoke.Play();
        }

        if (casingEjection != null)
        {
            ConfigureCasingEjection();
            casingEjection.Emit(1);
        }

        AudioClip soundToPlay = (isSuppressed && suppressedSound != null) ? suppressedSound : shootSound;
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }

        CharacterAnimatorController animator = characterController.GetComponent<CharacterAnimatorController>();
        if (animator != null)
        {
            animator.ApplyRecoil(recoilX, recoilY);
        }

        if (weaponType == WeaponType.Shotgun && numberOfPellets > 1)
        {
            for (int i = 0; i < numberOfPellets; i++)
            {
                PerformHitscan(true, i);
            }
        }
        else
        {
            PerformHitscan(false, 0);
        }
    }

    void PerformHitscan(bool isShotgunPellet = false, int pelletIndex = 0)
    {
        float currentSpread = bulletSpreadAngle;

        if (isShotgunPellet)
        {
            currentSpread = pelletSpreadAngle;
        }

        if (characterController != null && characterController.IsAiming())
        {
            currentSpread *= aimSpreadMultiplier;
        }

        Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 spreadDirection = centerRay.direction;

        if (isShotgunPellet)
        {
            Vector2 spreadOffset = GetSpreadOffset(pelletIndex, numberOfPellets, currentSpread);
            spreadDirection += playerCamera.transform.up * spreadOffset.y * 0.01f;
            spreadDirection += playerCamera.transform.right * spreadOffset.x * 0.01f;
        }
        else
        {
            spreadDirection += playerCamera.transform.up * Random.Range(-currentSpread, currentSpread) * 0.01f;
            spreadDirection += playerCamera.transform.right * Random.Range(-currentSpread, currentSpread) * 0.01f;
        }

        spreadDirection.Normalize();
        Ray ray = new Ray(centerRay.origin, spreadDirection);
        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, range))
        {
            targetPoint = hit.point;

            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            if (impactEffect != null && (!isShotgunPellet || pelletIndex == 0))
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
        }
        else
        {
            targetPoint = ray.GetPoint(range);
        }

        if (muzzlePoint != null)
        {
            if (!isShotgunPellet || pelletIndex % 2 == 0)
            {
                CreateBulletTracer(muzzlePoint.position, targetPoint);
            }
        }
    }

    Vector2 GetSpreadOffset(int pelletIndex, int totalPellets, float spreadAngle)
    {
        Vector2 offset = Vector2.zero;

        switch (spreadPattern)
        {
            case ShotgunSpreadPattern.Cone:
                float angle = (pelletIndex / (float)totalPellets) * 360f;
                float distance = Random.Range(0.3f, 1f) * spreadAngle;
                offset.x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
                offset.y = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
                break;

            case ShotgunSpreadPattern.Horizontal:
                float horizontalSpacing = (pelletIndex - totalPellets / 2f) / (float)totalPellets;
                offset.x = horizontalSpacing * spreadAngle * 2f;
                offset.y = Random.Range(-0.2f, 0.2f) * spreadAngle;
                break;

            case ShotgunSpreadPattern.Vertical:
                float verticalSpacing = (pelletIndex - totalPellets / 2f) / (float)totalPellets;
                offset.y = verticalSpacing * spreadAngle * 2f;
                offset.x = Random.Range(-0.2f, 0.2f) * spreadAngle;
                break;

            case ShotgunSpreadPattern.Cross:
                if (pelletIndex < totalPellets / 2)
                {
                    float crossH = (pelletIndex - totalPellets / 4f) / (float)(totalPellets / 2);
                    offset.x = crossH * spreadAngle;
                }
                else
                {
                    float crossV = ((pelletIndex - totalPellets / 2) - totalPellets / 4f) / (float)(totalPellets / 2);
                    offset.y = crossV * spreadAngle;
                }
                break;

            case ShotgunSpreadPattern.Random:
                offset.x = Random.Range(-spreadAngle, spreadAngle);
                offset.y = Random.Range(-spreadAngle, spreadAngle);
                break;
        }

        return offset;
    }

    void CreateBulletTracer(Vector3 start, Vector3 end)
    {
        GameObject tracerObj = new GameObject("BulletTracer_Line");
        LineRenderer line = tracerObj.AddComponent<LineRenderer>();

        line.positionCount = tracerSegments;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.SetColor("_Color", tracerColor);
        line.startColor = tracerColor;
        line.endColor = new Color(tracerColor.r, tracerColor.g * 0.5f, 0f, 0.8f);

        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, tracerBulbSize);
        widthCurve.AddKey(0.05f, tracerBulbSize);
        widthCurve.AddKey(0.15f, tracerBulbSize * 0.6f);
        widthCurve.AddKey(0.25f, tracerBulbSize * 0.3f);
        widthCurve.AddKey(1f, tracerEndWidth);
        line.widthCurve = widthCurve;

        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = 100;

        Vector3 direction = (end - start).normalized;
        for (int i = 0; i < tracerSegments; i++)
        {
            line.SetPosition(i, start);
        }

        StartCoroutine(AnimateThermometerTracer(line, start, end, direction));
    }

    System.Collections.IEnumerator AnimateThermometerTracer(LineRenderer line, Vector3 start, Vector3 end, Vector3 direction)
    {
        if (line == null) yield break;

        float distance = Vector3.Distance(start, end);
        float duration = distance / bulletVelocity;
        float elapsed = 0f;

        while (elapsed < duration && line != null)
        {
            float progress = elapsed / duration;
            float currentDistance = distance * progress;

            for (int i = 0; i < tracerSegments; i++)
            {
                float segmentPercent = (float)i / (tracerSegments - 1);
                float segmentDistance = currentDistance * segmentPercent;
                Vector3 segmentPos = start + direction * segmentDistance;
                line.SetPosition(i, segmentPos);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (line != null)
        {
            for (int i = 0; i < tracerSegments; i++)
            {
                float t = (float)i / (tracerSegments - 1);
                line.SetPosition(i, Vector3.Lerp(start, end, t));
            }
        }

        yield return new WaitForSeconds(tracerVisibleTime);

        if (line != null)
        {
            float fadeElapsed = 0f;
            Color startColorOriginal = line.startColor;
            Color endColorOriginal = line.endColor;

            while (fadeElapsed < tracerFadeTime && line != null)
            {
                float alpha = 1f - (fadeElapsed / tracerFadeTime);
                line.startColor = new Color(startColorOriginal.r, startColorOriginal.g, startColorOriginal.b, alpha);
                line.endColor = new Color(endColorOriginal.r, endColorOriginal.g, endColorOriginal.b, alpha * 0.5f);

                fadeElapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(line.gameObject);
        }
    }

    void UpdateWeaponRotation()
    {
        if (weaponPivot == null || playerCamera == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 targetRotation = new Vector3(
            mouseY * weaponRotationAmount,
            -mouseX * weaponRotationAmount,
            -mouseX * weaponRotationAmount * 0.3f
        );

        Quaternion targetQuaternion = Quaternion.Euler(targetRotation);

        weaponPivot.localRotation = Quaternion.Slerp(
            weaponPivot.localRotation,
            targetQuaternion,
            weaponRotationSpeed * Time.deltaTime
        );
    }

    public void Reload()
    {
        if (isReloading || currentAmmo == magSize || reserveAmmo <= 0)
            return;

        StartCoroutine(ReloadCoroutine());
    }

    System.Collections.IEnumerator ReloadCoroutine()
    {
        isReloading = true;

        if (reloadSound != null && audioSource != null)
            audioSource.PlayOneShot(reloadSound);

        if (useProceduralReload && weaponPivot != null)
        {
            // IMPORTANT: Use the ORIGINAL base position, not current (which has bob/sway)
            // This prevents snapping when reload ends
            Vector3 basePos = Vector3.zero; // Weapon pivot should start at (0,0,0) local
            Quaternion baseRot = Quaternion.identity;

            Vector3 reloadPos = basePos + reloadPositionOffset;
            Quaternion reloadRot = baseRot * Quaternion.Euler(reloadRotationOffset);

            float lowerDuration = reloadDuration * 0.15f;
            float holdDuration = reloadDuration * 0.70f;
            float raiseDuration = reloadDuration * 0.15f;

            // PHASE 1: Lower gun (to reload position)
            float elapsed = 0f;
            while (elapsed < lowerDuration)
            {
                float t = elapsed / lowerDuration;
                float curveT = reloadCurve.Evaluate(t);

                // Calculate OFFSET from base (additive animation)
                Vector3 currentOffset = Vector3.Lerp(Vector3.zero, reloadPositionOffset, curveT);
                Quaternion currentRotOffset = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(reloadRotationOffset), curveT);

                // Apply offset (CharacterAnimatorController will add bob/sway on top)
                weaponPivot.localPosition = basePos + currentOffset;
                weaponPivot.localRotation = baseRot * currentRotOffset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure we're at exact reload position
            weaponPivot.localPosition = reloadPos;
            weaponPivot.localRotation = reloadRot;

            // PHASE 2: Hold position
            yield return new WaitForSeconds(holdDuration);

            // PHASE 3: Raise gun (back to base)
            elapsed = 0f;
            while (elapsed < raiseDuration)
            {
                float t = elapsed / raiseDuration;
                // Use REVERSE curve for raising (smoother)
                float curveT = reloadCurve.Evaluate(1f - t);

                // Lerp from reload back to base
                Vector3 currentOffset = Vector3.Lerp(Vector3.zero, reloadPositionOffset, curveT);
                Quaternion currentRotOffset = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(reloadRotationOffset), curveT);

                weaponPivot.localPosition = basePos + currentOffset;
                weaponPivot.localRotation = baseRot * currentRotOffset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Return to exact base position
            weaponPivot.localPosition = basePos;
            weaponPivot.localRotation = baseRot;
        }
        else
        {
            yield return new WaitForSeconds(reloadDuration);
        }

        int ammoNeeded = magSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        isReloading = false;
    }

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public int GetReserveAmmo()
    {
        return reserveAmmo;
    }

    public bool IsReloading()
    {
        return isReloading;
    }

    void ApplyWeaponPreset()
    {
        switch (weaponType)
        {
            case WeaponType.AssaultRifle:
                magSize = 30;
                reserveAmmo = 90;
                fireRate = 0.1f;
                damage = 25f;
                range = 100f;
                bulletVelocity = 1000f;
                recoilX = 0.8f;
                recoilY = 3f;
                bulletSpreadAngle = 2f;
                aimSpreadMultiplier = 0.3f;
                weaponRotationAmount = 0.6f;
                tracerColor = new Color(1f, 0.9f, 0.3f);
                break;

            case WeaponType.SMG:
                magSize = 35;
                reserveAmmo = 140;
                fireRate = 0.06f;
                damage = 18f;
                range = 60f;
                bulletVelocity = 800f;
                recoilX = 1.5f;
                recoilY = 2f;
                bulletSpreadAngle = 4f;
                aimSpreadMultiplier = 0.5f;
                weaponRotationAmount = 0.7f;
                tracerColor = new Color(1f, 0.8f, 0.2f);
                break;

            case WeaponType.SniperRifle:
                magSize = 5;
                reserveAmmo = 20;
                fireRate = 1.2f;
                damage = 100f;
                range = 300f;
                bulletVelocity = 2000f;
                recoilX = 1.5f;
                recoilY = 12f;
                bulletSpreadAngle = 0.3f;
                aimSpreadMultiplier = 0.1f;
                weaponRotationAmount = 0.8f;
                tracerColor = new Color(0.3f, 0.8f, 1f);
                break;

            case WeaponType.Shotgun:
                magSize = 8;
                reserveAmmo = 32;
                fireRate = 0.8f;
                damage = 15f;
                range = 40f;
                bulletVelocity = 600f;
                recoilX = 2f;
                recoilY = 8f;
                bulletSpreadAngle = 8f;
                aimSpreadMultiplier = 0.6f;
                weaponRotationAmount = 0.9f;
                tracerColor = new Color(1f, 0.5f, 0f);
                numberOfPellets = 8;
                pelletSpreadAngle = 10f;
                spreadPattern = ShotgunSpreadPattern.Cone;
                break;

            case WeaponType.Pistol:
                magSize = 12;
                reserveAmmo = 60;
                fireRate = 0.2f;
                damage = 30f;
                range = 80f;
                bulletVelocity = 900f;
                recoilX = 1.2f;
                recoilY = 4f;
                bulletSpreadAngle = 1.5f;
                aimSpreadMultiplier = 0.2f;
                weaponRotationAmount = 0.5f;
                tracerColor = new Color(1f, 1f, 0.8f);
                break;

            case WeaponType.LMG:
                magSize = 100;
                reserveAmmo = 200;
                fireRate = 0.08f;
                damage = 28f;
                range = 120f;
                bulletVelocity = 950f;
                recoilX = 1f;
                recoilY = 2.5f;
                bulletSpreadAngle = 3f;
                aimSpreadMultiplier = 0.4f;
                weaponRotationAmount = 0.4f;
                tracerColor = new Color(1f, 0.6f, 0f);
                break;
        }
    }

    void SetupLaserSight()
    {
        if (laserLine != null && laserLine.gameObject != null)
        {
            Destroy(laserLine.gameObject);
            laserLine = null;
        }

        if (laserDot != null)
        {
            Destroy(laserDot);
            laserDot = null;
        }

        if (laserOrigin == null)
        {
            return;
        }

        GameObject laserObj = new GameObject("LaserSight");
        laserObj.transform.SetParent(laserOrigin);
        laserObj.transform.localPosition = Vector3.zero;
        laserObj.transform.localRotation = Quaternion.identity;

        laserLine = laserObj.AddComponent<LineRenderer>();
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        laserLine.material = new Material(Shader.Find("Unlit/Color"));
        laserLine.material.SetColor("_Color", laserColor);
        laserLine.startColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.3f);
        laserLine.endColor = new Color(laserColor.r, laserColor.g, laserColor.b, 0.1f);
        laserLine.positionCount = 2;
        laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laserLine.useWorldSpace = true; // Important: use world space for line positions

        laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        laserDot.name = "LaserDot";
        laserDot.transform.localScale = Vector3.one * 0.05f;
        Destroy(laserDot.GetComponent<Collider>());

        Renderer dotRenderer = laserDot.GetComponent<Renderer>();
        dotRenderer.material = new Material(Shader.Find("Unlit/Color"));
        dotRenderer.material.SetColor("_Color", laserColor);
        dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void UpdateLaser()
    {
        if (laserLine == null || laserOrigin == null) return;

        Vector3 start = laserOrigin.position;
        Vector3 direction = laserOrigin.forward;

        laserLine.SetPosition(0, start);

        if (Physics.Raycast(start, direction, out RaycastHit hit, laserMaxDistance))
        {
            laserLine.SetPosition(1, hit.point);
            if (laserDot != null)
            {
                laserDot.SetActive(true);
                laserDot.transform.position = hit.point + hit.normal * 0.01f;
                laserDot.transform.rotation = Quaternion.LookRotation(hit.normal);
            }
        }
        else
        {
            laserLine.SetPosition(1, start + direction * laserMaxDistance);
            if (laserDot != null) laserDot.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (laserLine != null && laserLine.gameObject != null)
        {
            Destroy(laserLine.gameObject);
        }

        if (laserDot != null)
        {
            Destroy(laserDot);
        }
    }
}

public interface IDamageable
{
    void TakeDamage(float damage);
}
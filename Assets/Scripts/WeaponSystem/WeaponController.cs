using UnityEngine;

public class WeaponController : MonoBehaviour
{
    private WeaponData weaponData;
    private DualPointRecoil recoilSystem;
    private AudioSource audioSource;

    private int currentAmmo;
    private int reserveAmmo;
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private float fireTimer = 0f;

    private ParticleSystem muzzleFlashEffect;
    private ParticleSystem muzzleSmokeEffect;
    private ParticleSystem casingEjectionEffect;
    private LaserSight laserSight;

    void Awake()
    {
        recoilSystem = GetComponent<DualPointRecoil>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        SetupEffects();
    }

    void Update()
    {
        if (weaponData == null) return;
        fireTimer += Time.deltaTime;

        if (isReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= weaponData.reloadTime)
                CompleteReload();
        }
    }

    public void Initialize(WeaponData data)
    {
        if (data == null) return;

        weaponData = data;
        currentAmmo = data.magazineSize;
        reserveAmmo = data.maxReserveAmmo;
        fireTimer = 0f;

        ApplyWeaponPosition();
        SetupEffects();
    }

    void ApplyWeaponPosition()
    {
        if (weaponData.hipFirePosition.position != Vector3.zero ||
            weaponData.hipFirePosition.rotation != Vector3.zero)
        {
            transform.localPosition = weaponData.hipFirePosition.position;
            transform.localRotation = Quaternion.Euler(weaponData.hipFirePosition.rotation);
        }
    }

    void SetupEffects()
    {
        if (weaponData == null) return;

        // === MUZZLE FLASH SETUP ===
        if (recoilSystem != null && recoilSystem.muzzlePoint != null && weaponData.muzzleFlashPrefab != null)
        {
            GameObject flashObj = Instantiate(weaponData.muzzleFlashPrefab, recoilSystem.muzzlePoint);
            flashObj.transform.localPosition = Vector3.zero;
            flashObj.transform.localRotation = Quaternion.identity;
            muzzleFlashEffect = flashObj.GetComponent<ParticleSystem>();

            if (muzzleFlashEffect != null)
            {
                var main = muzzleFlashEffect.main;
                main.playOnAwake = false;
                main.loop = false;
                muzzleFlashEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Debug.Log("[WeaponController] Muzzle flash setup complete");
            }
        }
        else
        {
            Debug.LogWarning("[WeaponController] Muzzle flash not setup - missing prefab or muzzle point");
        }

        // === MUZZLE SMOKE SETUP ===
        if (recoilSystem != null && recoilSystem.muzzlePoint != null && weaponData.muzzleSmokePrefab != null)
        {
            GameObject smokeObj = Instantiate(weaponData.muzzleSmokePrefab, recoilSystem.muzzlePoint);
            smokeObj.transform.localPosition = Vector3.zero;
            smokeObj.transform.localRotation = Quaternion.identity;
            muzzleSmokeEffect = smokeObj.GetComponent<ParticleSystem>();

            if (muzzleSmokeEffect != null)
            {
                var main = muzzleSmokeEffect.main;
                main.playOnAwake = false;
                main.loop = false;

                // Just use the smoke as-is from the prefab
                // Don't modify velocity curves - they're already set up correctly

                muzzleSmokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Debug.Log($"[WeaponController] Muzzle smoke setup complete (Attachment: {weaponData.barrelAttachment})");
            }
        }

        // === LASER SIGHT SETUP ===
        // Handled by AttachmentManager now - removed from here

        // === CASING EJECTION SETUP ===
        Transform casingPoint = transform.Find("CasingEjectionPoint");
        if (casingPoint == null)
            casingPoint = transform.Find("CasingEjection");

        // Try recursive search if not found
        if (casingPoint == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "CasingEjectionPoint" || child.name == "CasingEjection")
                {
                    casingPoint = child;
                    break;
                }
            }
        }

        if (casingPoint != null && weaponData.casingPrefab != null)
        {
            GameObject casingObj = Instantiate(weaponData.casingPrefab, casingPoint);
            casingObj.transform.localPosition = Vector3.zero;
            casingObj.transform.localRotation = Quaternion.identity;
            casingEjectionEffect = casingObj.GetComponent<ParticleSystem>();

            if (casingEjectionEffect != null)
            {
                casingEjectionEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Debug.Log("[WeaponController] Casing ejection setup complete at: " + casingPoint.name);
            }
            else
            {
                Debug.LogWarning("[WeaponController] Casing prefab has no ParticleSystem component!");
            }
        }
        else
        {
            if (casingPoint == null)
                Debug.LogWarning("[WeaponController] No CasingEjectionPoint found on weapon! Searched entire hierarchy.");
            if (weaponData.casingPrefab == null)
                Debug.LogWarning("[WeaponController] No casing prefab assigned in WeaponData!");
        }
    }

    float GetSmokeVelocityMultiplier()
    {
        switch (weaponData.barrelAttachment)
        {
            case WeaponData.BarrelAttachment.Suppressor:
                return 0.3f; // Very little smoke
            case WeaponData.BarrelAttachment.FlashHider:
                return 0.7f; // Some smoke
            case WeaponData.BarrelAttachment.MuzzleBrake:
                return 1.5f; // Lots of smoke, more velocity
            default:
                return 1f; // Normal
        }
    }

    bool CanFire()
    {
        return currentAmmo > 0 && fireTimer >= 60f / weaponData.fireRate && !isReloading;
    }

    void Fire()
    {
        currentAmmo--;
        fireTimer = 0f;

        PlayMuzzleFlash();
        EjectCasing();
        PlayFireSound();

        // Apply weapon visual recoil
        if (recoilSystem != null)
            recoilSystem.ApplyRecoil(weaponData.recoilPattern);

        // Apply camera recoil with weapon-specific values
        FPSCharacterController fpsController = FindFirstObjectByType<FPSCharacterController>();
        if (fpsController != null && fpsController.GetCameraRecoil() != null)
        {
            fpsController.GetCameraRecoil().ApplyRecoil(
                weaponData.cameraVerticalRecoil,
                weaponData.cameraHorizontalRecoil,
                weaponData.cameraHorizontalVariance,
                weaponData.recoilResistance // Pass weapon's resistance value
            );
        }

        if (weaponData.isShotgun && weaponData.enableShotgunSpread)
            FireShotgunPellets();
        else
            FireSingleRound();

        if (currentAmmo <= 0 && reserveAmmo > 0)
            StartReload();
    }

    void FireSingleRound()
    {
        Vector3 raycastOrigin = recoilSystem != null ? recoilSystem.GetMuzzlePosition() : transform.position;
        Vector3 shootDirection = recoilSystem != null ? recoilSystem.GetMuzzleDirection() : transform.forward;

        // Apply weapon spread
        float spread = (1f - weaponData.accuracy) * 5f;
        shootDirection = ApplySpread(shootDirection, spread);

        int layerMask = ~LayerMask.GetMask("Player");

        RaycastHit hit;
        Vector3 hitPoint;
        bool didHit = false;
        Vector3[] pathPoints = null;

        // Use ballistic physics if enabled
        if (weaponData.useBallisticPhysics)
        {
            didHit = BallisticTrajectory.FireBallisticRay(
                raycastOrigin,
                shootDirection,
                weaponData.bulletVelocity,
                weaponData.bulletGravity,
                weaponData.range,
                weaponData.trajectorySegments,
                layerMask,
                out hit,
                out pathPoints
            );

            if (didHit)
            {
                hitPoint = hit.point;
                float distance = Vector3.Distance(raycastOrigin, hitPoint);

                // Apply damage falloff
                float finalDamage = BallisticTrajectory.CalculateDamageFalloff(
                    distance,
                    weaponData.damageFalloffCurve,
                    weaponData.damage
                );

                Debug.Log($"[BALLISTIC HIT] Target: {hit.collider.name} | Distance: {distance:F1}m | Damage: {finalDamage:F1}/{weaponData.damage} | Hit Point: {hitPoint}");
                SpawnImpactEffect(hit);
            }
            else
            {
                if (pathPoints != null && pathPoints.Length > 0)
                {
                    hitPoint = pathPoints[pathPoints.Length - 1];
                    Debug.Log($"[BALLISTIC MISS] End point: {hitPoint} | Path segments: {pathPoints.Length}");
                }
                else
                {
                    hitPoint = raycastOrigin + shootDirection * weaponData.range;
                    Debug.LogWarning("[BALLISTIC ERROR] No path points generated!");
                }
            }

            // Draw ballistic path for debugging
            if (pathPoints != null && pathPoints.Length > 1)
            {
                for (int i = 0; i < pathPoints.Length - 1; i++)
                {
                    Debug.DrawLine(pathPoints[i], pathPoints[i + 1], didHit ? Color.red : Color.yellow, 2f);
                }
            }
        }
        else
        {
            // Standard straight raycast
            if (Physics.Raycast(raycastOrigin, shootDirection, out hit, weaponData.range, layerMask))
            {
                hitPoint = hit.point;
                didHit = true;
                Debug.DrawLine(raycastOrigin, hit.point, Color.red, 0.5f);
                SpawnImpactEffect(hit);
            }
            else
            {
                hitPoint = raycastOrigin + shootDirection * weaponData.range;
                Debug.DrawLine(raycastOrigin, hitPoint, Color.yellow, 0.5f);
            }
        }

        // Spawn tracer
        if (weaponData.tracerPrefab != null)
        {
            if (weaponData.useBallisticPhysics && pathPoints != null)
            {
                SpawnBallisticTracer(pathPoints);
            }
            else
            {
                SpawnTracer(raycastOrigin, hitPoint);
            }
        }
    }

    void SpawnBallisticTracer(Vector3[] pathPoints)
    {
        // Create tracer that follows ballistic path
        GameObject tracer = Instantiate(weaponData.tracerPrefab);
        tracer.transform.position = pathPoints[0];

        StartCoroutine(AnimateBallisticTracer(tracer, pathPoints));
    }

    System.Collections.IEnumerator AnimateBallisticTracer(GameObject tracer, Vector3[] pathPoints)
    {
        float duration = 0.1f; // Total travel time
        float elapsed = 0f;
        int currentSegment = 0;

        while (elapsed < duration && currentSegment < pathPoints.Length - 1)
        {
            elapsed += Time.deltaTime;
            float totalProgress = elapsed / duration;
            int targetSegment = Mathf.FloorToInt(totalProgress * (pathPoints.Length - 1));

            // Move through segments
            while (currentSegment < targetSegment && currentSegment < pathPoints.Length - 1)
            {
                currentSegment++;
            }

            // Interpolate within current segment
            if (currentSegment < pathPoints.Length - 1)
            {
                float segmentProgress = (totalProgress * (pathPoints.Length - 1)) - currentSegment;
                tracer.transform.position = Vector3.Lerp(
                    pathPoints[currentSegment],
                    pathPoints[currentSegment + 1],
                    segmentProgress
                );

                Vector3 direction = (pathPoints[currentSegment + 1] - pathPoints[currentSegment]).normalized;
                if (direction != Vector3.zero)
                {
                    tracer.transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            yield return null;
        }

        Destroy(tracer, 0.5f);
    }

    void SpawnTracer(Vector3 start, Vector3 end)
    {
        GameObject tracer = Instantiate(weaponData.tracerPrefab);
        tracer.transform.position = start;
        tracer.transform.LookAt(end);

        // Animate tracer movement
        StartCoroutine(AnimateTracer(tracer, start, end));
    }

    System.Collections.IEnumerator AnimateTracer(GameObject tracer, Vector3 start, Vector3 end)
    {
        float duration = 0.1f; // Tracer travel time
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            tracer.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        Destroy(tracer, 0.5f); // Let particle effects finish
    }

    void FireShotgunPellets()
    {
        Vector3 raycastOrigin = recoilSystem != null ? recoilSystem.GetMuzzlePosition() : transform.position;
        Vector3 baseDirection = recoilSystem != null ? recoilSystem.GetMuzzleDirection() : transform.forward;

        int layerMask = ~LayerMask.GetMask("Player");

        for (int i = 0; i < weaponData.pelletsPerShot; i++)
        {
            Vector3 direction = ApplySpread(baseDirection, weaponData.spreadAngle);

            RaycastHit hit;
            if (Physics.Raycast(raycastOrigin, direction, out hit, weaponData.range, layerMask))
            {
                Debug.DrawLine(raycastOrigin, hit.point, Color.red, 0.5f);
                SpawnImpactEffect(hit);
            }
            else
            {
                Vector3 endPoint = raycastOrigin + direction * weaponData.range;
                Debug.DrawLine(raycastOrigin, endPoint, Color.yellow, 0.5f);
            }
        }
    }

    Vector3 ApplySpread(Vector3 direction, float angle)
    {
        float spreadX = Random.Range(-angle, angle);
        float spreadY = Random.Range(-angle, angle);
        return Quaternion.Euler(spreadX, spreadY, 0) * direction;
    }

    void PlayMuzzleFlash()
    {
        if (muzzleFlashEffect != null)
        {
            muzzleFlashEffect.Play();
        }

        if (muzzleSmokeEffect != null)
        {
            muzzleSmokeEffect.Emit(weaponData.smokeParticlesPerShot);
        }
    }

    void EjectCasing()
    {
        if (casingEjectionEffect != null)
            casingEjectionEffect.Emit(1);
    }

    void PlayFireSound()
    {
        if (audioSource != null && weaponData.fireSound != null)
            audioSource.PlayOneShot(weaponData.fireSound);
    }

    void SpawnImpactEffect(RaycastHit hit)
    {
        if (weaponData.impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(weaponData.impactEffectPrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f);
        }
    }

    public bool TryShoot()
    {
        if (CanFire())
        {
            Fire();
            return true;
        }
        return false;
    }

    public void Reload()
    {
        if (currentAmmo < weaponData.magazineSize && reserveAmmo > 0 && !isReloading)
            StartReload();
    }

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo = Mathf.Min(reserveAmmo + amount, weaponData.maxReserveAmmo);
    }

    void StartReload()
    {
        isReloading = true;
        reloadTimer = 0f;
        if (audioSource != null && weaponData.reloadSound != null)
            audioSource.PlayOneShot(weaponData.reloadSound);
    }

    void CompleteReload()
    {
        int ammoNeeded = weaponData.magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;
        isReloading = false;
        reloadTimer = 0f;
    }

    public bool IsReloading() => isReloading;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public WeaponData GetWeaponData() => weaponData;
}
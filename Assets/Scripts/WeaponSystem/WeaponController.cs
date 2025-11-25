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

        // === MUZZLE FLASH ===
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
            }
        }

        // === MUZZLE SMOKE ===
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
                muzzleSmokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // === CASING EJECTION ===
        Transform casingPoint = transform.Find("CasingEjectionPoint");
        if (casingPoint == null)
            casingPoint = transform.Find("CasingEjection");

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
                casingEjectionEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        // Weapon visual recoil
        if (recoilSystem != null)
            recoilSystem.ApplyRecoil(weaponData.recoilPattern);

        // Camera recoil
        FPSCharacterController fpsController = FindFirstObjectByType<FPSCharacterController>();
        if (fpsController != null && fpsController.GetCameraRecoil() != null)
        {
            fpsController.GetCameraRecoil().ApplyRecoil(
                weaponData.cameraVerticalRecoil,
                weaponData.cameraHorizontalRecoil,
                weaponData.cameraHorizontalVariance,
                weaponData.recoilResistance
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
        Vector3 muzzlePos = recoilSystem != null ? recoilSystem.GetMuzzlePosition() : transform.position;
        Vector3 shootDir = recoilSystem != null ? recoilSystem.GetMuzzleDirection() : transform.forward;

        float spread = (1f - weaponData.accuracy) * 5f;
        shootDir = ApplySpread(shootDir, spread);

        int layerMask = ~LayerMask.GetMask("Player");

        RaycastHit hit;
        Vector3 hitPoint;
        bool didHit = false;
        Vector3[] pathPoints = null;

        if (weaponData.useBallisticPhysics)
        {
            // === NEW SIMPLE BALLISTICS ===
            didHit = BulletPhysics.FireBullet(
                muzzlePos,
                shootDir,
                weaponData,
                layerMask,
                out hit,
                out pathPoints
            );

            if (didHit)
            {
                hitPoint = hit.point;
                float distance = Vector3.Distance(muzzlePos, hitPoint);
                float actualDamage = BulletPhysics.CalculateDamage(weaponData, distance);

                SpawnImpactEffect(hit);
                Debug.Log($"[BALLISTIC HIT] {hit.collider.name} at {distance:F1}m | Damage: {actualDamage:F1}");
            }
            else
            {
                hitPoint = pathPoints != null && pathPoints.Length > 0
                    ? pathPoints[pathPoints.Length - 1]
                    : muzzlePos + shootDir * weaponData.range;
            }

            // Debug draw trajectory
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
            if (Physics.Raycast(muzzlePos, shootDir, out hit, weaponData.range, layerMask))
            {
                hitPoint = hit.point;
                didHit = true;
                Debug.DrawLine(muzzlePos, hit.point, Color.red, 0.5f);
                SpawnImpactEffect(hit);
            }
            else
            {
                hitPoint = muzzlePos + shootDir * weaponData.range;
                Debug.DrawLine(muzzlePos, hitPoint, Color.yellow, 0.5f);
            }
        }

        // Spawn tracer
        if (weaponData.tracerPrefab != null)
        {
            if (weaponData.useBallisticPhysics && pathPoints != null && pathPoints.Length > 1)
                SpawnBallisticTracer(pathPoints);
            else
                SpawnTracer(muzzlePos, hitPoint);
        }
    }

    void SpawnBallisticTracer(Vector3[] pathPoints)
    {
        GameObject tracer = Instantiate(weaponData.tracerPrefab);
        tracer.transform.position = pathPoints[0];
        StartCoroutine(AnimateBallisticTracer(tracer, pathPoints));
    }

    System.Collections.IEnumerator AnimateBallisticTracer(GameObject tracer, Vector3[] pathPoints)
    {
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration && tracer != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            float pathProgress = progress * (pathPoints.Length - 1);
            int segmentIndex = Mathf.Clamp(Mathf.FloorToInt(pathProgress), 0, pathPoints.Length - 2);
            float segmentProgress = pathProgress - segmentIndex;

            tracer.transform.position = Vector3.Lerp(
                pathPoints[segmentIndex],
                pathPoints[segmentIndex + 1],
                segmentProgress
            );

            Vector3 direction = (pathPoints[segmentIndex + 1] - pathPoints[segmentIndex]).normalized;
            if (direction != Vector3.zero)
                tracer.transform.rotation = Quaternion.LookRotation(direction);

            yield return null;
        }

        if (tracer != null)
            Destroy(tracer, 0.3f);
    }

    void SpawnTracer(Vector3 start, Vector3 end)
    {
        GameObject tracer = Instantiate(weaponData.tracerPrefab);
        tracer.transform.position = start;
        tracer.transform.LookAt(end);
        StartCoroutine(AnimateTracer(tracer, start, end));
    }

    System.Collections.IEnumerator AnimateTracer(GameObject tracer, Vector3 start, Vector3 end)
    {
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration && tracer != null)
        {
            elapsed += Time.deltaTime;
            tracer.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        if (tracer != null)
            Destroy(tracer, 0.3f);
    }

    void FireShotgunPellets()
    {
        Vector3 muzzlePos = recoilSystem != null ? recoilSystem.GetMuzzlePosition() : transform.position;
        Vector3 baseDir = recoilSystem != null ? recoilSystem.GetMuzzleDirection() : transform.forward;

        int layerMask = ~LayerMask.GetMask("Player");

        for (int i = 0; i < weaponData.pelletsPerShot; i++)
        {
            Vector3 direction = ApplySpread(baseDir, weaponData.spreadAngle);

            RaycastHit hit;
            if (Physics.Raycast(muzzlePos, direction, out hit, weaponData.range, layerMask))
            {
                Debug.DrawLine(muzzlePos, hit.point, Color.red, 0.5f);
                SpawnImpactEffect(hit);
            }
            else
            {
                Debug.DrawLine(muzzlePos, muzzlePos + direction * weaponData.range, Color.yellow, 0.5f);
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
            muzzleFlashEffect.Play();

        if (muzzleSmokeEffect != null)
            muzzleSmokeEffect.Emit(weaponData.smokeParticlesPerShot);
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
            GameObject impact = Instantiate(
                weaponData.impactEffectPrefab,
                hit.point + hit.normal * 0.01f,
                Quaternion.LookRotation(hit.normal)
            );
            Destroy(impact, 2f);
        }
    }

    // === PUBLIC API ===

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
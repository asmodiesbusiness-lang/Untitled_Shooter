using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("=== WEAPON DATA ===")]
    private WeaponData weaponData;

    [Header("=== COMPONENTS ===")]
    private DualPointRecoil recoilSystem;

    [Header("=== AMMO ===")]
    private int currentAmmo;
    private int reserveAmmo;

    [Header("=== STATE ===")]
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private float fireTimer = 0f;

    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform muzzlePoint;

    // Debug flags
    private bool hasLoggedSetup = false;

    void Awake()
    {
        Debug.Log($"[WeaponController] ========== AWAKE ==========");
        Debug.Log($"[WeaponController] GameObject: {gameObject.name}");
        Debug.Log($"[WeaponController] Position: {transform.position}");
        Debug.Log($"[WeaponController] Parent: {transform.parent?.name ?? "None"}");

        recoilSystem = GetComponent<DualPointRecoil>();
        if (recoilSystem == null)
            Debug.LogError("[WeaponController] ❌ No DualPointRecoil component found!");
        else
            Debug.Log("[WeaponController] ✅ DualPointRecoil found");

        if (muzzlePoint == null)
        {
            muzzlePoint = transform.Find("MuzzlePoint");
            if (muzzlePoint == null)
                Debug.LogWarning("[WeaponController] ⚠️ No MuzzlePoint found - shooting won't work properly!");
            else
                Debug.Log($"[WeaponController] ✅ MuzzlePoint found at {muzzlePoint.localPosition}");
        }
    }

    void Start()
    {
        Debug.Log($"[WeaponController] ========== START ==========");
        if (weaponData == null)
            Debug.LogError("[WeaponController] ❌ WeaponData is NULL - Initialize() was not called!");
        else
            Debug.Log($"[WeaponController] ✅ WeaponData: {weaponData.weaponName}");
    }

    void Update()
    {
        if (weaponData == null)
        {
            if (!hasLoggedSetup)
            {
                Debug.LogError("[WeaponController] ❌ No WeaponData - weapon won't function!");
                hasLoggedSetup = true;
            }
            return;
        }

        fireTimer += Time.deltaTime;

        // Reload handling
        if (isReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= weaponData.reloadTime)
            {
                CompleteReload();
            }
            return;
        }

        // Fire input
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log($"[WeaponController] 🔫 Fire1 pressed! CanFire: {CanFire()}, Ammo: {currentAmmo}, FireTimer: {fireTimer:F2}, Required: {60f / weaponData.fireRate:F2}");
            if (!CanFire())
            {
                if (currentAmmo <= 0)
                    Debug.Log("[WeaponController] ❌ Can't fire - No ammo!");
                else if (fireTimer < 60f / weaponData.fireRate)
                    Debug.Log($"[WeaponController] ❌ Can't fire - Too soon! Wait {60f / weaponData.fireRate - fireTimer:F2}s");
                else if (isReloading)
                    Debug.Log("[WeaponController] ❌ Can't fire - Reloading!");
            }
        }

        if (Input.GetButton("Fire1") && CanFire())
        {
            Fire();
        }

        // Reload input
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log($"[WeaponController] 🔄 R pressed! Current: {currentAmmo}/{weaponData.magazineSize}, Reserve: {reserveAmmo}");
            if (currentAmmo >= weaponData.magazineSize)
                Debug.Log("[WeaponController] ❌ Can't reload - Magazine full!");
            else if (reserveAmmo <= 0)
                Debug.Log("[WeaponController] ❌ Can't reload - No reserve ammo!");
            else if (isReloading)
                Debug.Log("[WeaponController] ❌ Already reloading!");
            else
                StartReload();
        }
    }

    public void Initialize(WeaponData data)
    {
        Debug.Log($"[WeaponController] ========== INITIALIZE ==========");

        if (data == null)
        {
            Debug.LogError("[WeaponController] ❌ Initialize called with NULL WeaponData!");
            return;
        }

        weaponData = data;
        currentAmmo = data.magazineSize;
        reserveAmmo = data.maxReserveAmmo;
        fireTimer = 0f;

        Debug.Log($"[WeaponController] ✅ Initialized with: {weaponData.weaponName}");
        Debug.Log($"[WeaponController] 📊 Stats: Damage={data.damage}, FireRate={data.fireRate}, Range={data.range}");
        Debug.Log($"[WeaponController] 🔫 Ammo: {currentAmmo}/{data.magazineSize} (Reserve: {reserveAmmo})");
        Debug.Log($"[WeaponController] 📍 Positioning: HipFire={data.hipFirePosition.position}, ADS={data.adsPosition.position}");

        // Check if positioning is being applied
        if (data.hipFirePosition.position != Vector3.zero)
            Debug.Log($"[WeaponController] 📍 HipFire position set to: {data.hipFirePosition.position}");
        else
            Debug.LogWarning("[WeaponController] ⚠️ HipFire position is zero - weapon might be at origin!");
    }

    bool CanFire()
    {
        return currentAmmo > 0 && fireTimer >= 60f / weaponData.fireRate && !isReloading;
    }

    void Fire()
    {
        currentAmmo--;
        fireTimer = 0f;

        Debug.Log($"[WeaponController] 💥 FIRING! Ammo now: {currentAmmo}/{weaponData.magazineSize}");

        if (recoilSystem != null)
        {
            recoilSystem.ApplyRecoil();
            Debug.Log("[WeaponController] ✅ Recoil applied");
        }
        else
        {
            Debug.LogWarning("[WeaponController] ⚠️ No recoil system!");
        }

        if (muzzlePoint != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out hit, weaponData.range))
            {
                Debug.DrawLine(muzzlePoint.position, hit.point, Color.red, 0.1f);
                Debug.Log($"[WeaponController] 🎯 HIT: {hit.collider.name} at {hit.distance:F1}m");
            }
            else
            {
                Debug.Log($"[WeaponController] ❌ No hit (max range: {weaponData.range}m)");
            }
        }
        else
        {
            Debug.LogError("[WeaponController] ❌ No muzzle point - can't raycast!");
        }

        if (currentAmmo <= 0 && reserveAmmo > 0)
        {
            Debug.Log("[WeaponController] 📢 Auto-reloading (out of ammo)");
            StartReload();
        }
    }

    public bool TryShoot()
    {
        Debug.Log($"[WeaponController] TryShoot called - CanFire: {CanFire()}");
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
        Debug.Log($"[WeaponController] ➕ Added {amount} ammo. Reserve now: {reserveAmmo}");
    }

    void StartReload()
    {
        isReloading = true;
        reloadTimer = 0f;
        Debug.Log($"[WeaponController] 🔄 RELOADING... (will take {weaponData.reloadTime}s)");
    }

    void CompleteReload()
    {
        int ammoNeeded = weaponData.magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        isReloading = false;
        reloadTimer = 0f;

        Debug.Log($"[WeaponController] ✅ RELOAD COMPLETE! Loaded {ammoToReload} rounds");
        Debug.Log($"[WeaponController] 📊 Ammo now: {currentAmmo}/{weaponData.magazineSize} (Reserve: {reserveAmmo})");
    }

    void OnEnable()
    {
        Debug.Log($"[WeaponController] ✅ Weapon enabled: {gameObject.name}");
    }

    void OnDisable()
    {
        Debug.Log($"[WeaponController] ❌ Weapon disabled: {gameObject.name}");
    }

    public bool IsReloading() => isReloading;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public WeaponData GetWeaponData() => weaponData;
}
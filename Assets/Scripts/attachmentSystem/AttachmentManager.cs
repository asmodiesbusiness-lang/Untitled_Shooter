using UnityEngine;

/// <summary>
/// Manages all weapon attachments - optics, barrel, laser, grip, mag, stock
/// Each attachment slot is optional and modifies weapon stats when equipped
/// </summary>
public class AttachmentManager : MonoBehaviour
{
    [Header("=== ATTACHMENT SLOTS ===")]
    [Tooltip("Optic/Scope attachment")]
    public ScopeData equippedOptic;

    [Tooltip("Barrel attachment (suppressor, brake, etc)")]
    public BarrelAttachmentData equippedBarrel;

    [Tooltip("Laser sight attachment")]
    public LaserData equippedLaser;

    // TODO: Add these later
    // public GripData equippedGrip;
    // public MagazineData equippedMagazine;
    // public StockData equippedStock;

    [Header("=== MOUNT POINTS ===")]
    [Tooltip("Where scope/optic attaches")]
    public Transform scopeMountPoint;

    [Tooltip("Where barrel attachment attaches (muzzle)")]
    public Transform barrelMountPoint;

    [Tooltip("Where laser attaches (usually underbarrel)")]
    public Transform laserMountPoint;

    [Header("=== REFERENCES ===")]
    private WeaponData baseWeaponData;
    private WeaponController weaponController;
    private DualPointRecoil recoilSystem;

    // Spawned attachment objects
    private GameObject spawnedOpticModel;
    private GameObject spawnedBarrelModel;
    private GameObject spawnedLaserObject;

    void Awake()
    {
        weaponController = GetComponent<WeaponController>();
        recoilSystem = GetComponent<DualPointRecoil>();

        // Auto-find mount points if not assigned
        AutoFindMountPoints();
    }

    void Start()
    {
        ApplyAllAttachments();
    }

    void AutoFindMountPoints()
    {
        if (scopeMountPoint == null)
            scopeMountPoint = transform.Find("ScopeMount");

        if (barrelMountPoint == null)
        {
            barrelMountPoint = transform.Find("MuzzlePoint");
            if (barrelMountPoint == null && recoilSystem != null)
                barrelMountPoint = recoilSystem.muzzlePoint;
        }

        if (laserMountPoint == null)
            laserMountPoint = transform.Find("LaserMount");
    }

    /// <summary>
    /// Apply all equipped attachments (called on weapon equip or attachment change)
    /// </summary>
    public void ApplyAllAttachments()
    {
        ApplyOptic();
        ApplyBarrelAttachment();
        ApplyLaserSight();

        // Recalculate weapon stats with all modifiers
        RecalculateWeaponStats();
    }

    void ApplyOptic()
    {
        // Destroy old optic if exists
        if (spawnedOpticModel != null)
            Destroy(spawnedOpticModel);

        if (equippedOptic == null || scopeMountPoint == null)
            return;

        // Spawn optic model
        if (equippedOptic.scopeModelPrefab != null)
        {
            spawnedOpticModel = Instantiate(equippedOptic.scopeModelPrefab, scopeMountPoint);
            spawnedOpticModel.transform.localPosition = equippedOptic.modelPositionOffset;
            spawnedOpticModel.transform.localRotation = Quaternion.Euler(equippedOptic.modelRotationOffset);
        }

        Debug.Log($"[AttachmentManager] Equipped optic: {equippedOptic.scopeName}");
    }

    void ApplyBarrelAttachment()
    {
        // Destroy old barrel attachment if exists
        if (spawnedBarrelModel != null)
            Destroy(spawnedBarrelModel);

        if (equippedBarrel == null || barrelMountPoint == null)
            return;

        // Spawn barrel model
        if (equippedBarrel.attachmentModelPrefab != null)
        {
            spawnedBarrelModel = Instantiate(equippedBarrel.attachmentModelPrefab, barrelMountPoint);
            spawnedBarrelModel.transform.localPosition = equippedBarrel.modelPosition;
            spawnedBarrelModel.transform.localRotation = Quaternion.Euler(equippedBarrel.modelRotation);
        }

        Debug.Log($"[AttachmentManager] Equipped barrel: {equippedBarrel.attachmentName}");
    }

    void ApplyLaserSight()
    {
        // Destroy old laser if exists
        if (spawnedLaserObject != null)
            Destroy(spawnedLaserObject);

        if (equippedLaser == null)
            return;

        // Create laser GameObject with LaserSight component
        Transform mountPoint = laserMountPoint != null ? laserMountPoint : transform;
        spawnedLaserObject = new GameObject("LaserSight");
        spawnedLaserObject.transform.SetParent(mountPoint);

        // Add LaserSight component and configure it with ALL LaserData settings
        LaserSight laserComponent = spawnedLaserObject.AddComponent<LaserSight>();
        laserComponent.ConfigureFromData(equippedLaser);

        Debug.Log($"[AttachmentManager] Equipped laser: {equippedLaser.laserName} with full day/night visibility");
    }

    /// <summary>
    /// Recalculate weapon stats based on all equipped attachments
    /// </summary>
    void RecalculateWeaponStats()
    {
        if (baseWeaponData == null)
            return;

        // Start with base weapon stats
        WeaponStats modifiedStats = new WeaponStats(baseWeaponData);

        // Apply barrel modifiers
        if (equippedBarrel != null)
        {
            modifiedStats.verticalRecoil *= equippedBarrel.verticalRecoilMultiplier;
            modifiedStats.horizontalRecoil *= equippedBarrel.horizontalRecoilMultiplier;
            modifiedStats.cameraRecoil *= equippedBarrel.cameraRecoilMultiplier;
            modifiedStats.kickback *= equippedBarrel.kickbackMultiplier;
            modifiedStats.range *= equippedBarrel.rangeMultiplier;
            modifiedStats.adsSpeed *= equippedBarrel.adsSpeedMultiplier;
        }

        // Apply laser modifiers
        if (equippedLaser != null)
        {
            modifiedStats.hipFireAccuracy += equippedLaser.hipFireAccuracyBonus;
            modifiedStats.adsSpeed *= equippedLaser.adsSpeedMultiplier;
        }

        // TODO: Apply grip, mag, stock modifiers when added

        Debug.Log($"[AttachmentManager] Stats recalculated - Recoil: {modifiedStats.verticalRecoil:F2}");
    }

    /// <summary>
    /// Get the total recoil multiplier from all attachments
    /// </summary>
    public Vector2 GetRecoilMultiplier()
    {
        float vertical = 1f;
        float horizontal = 1f;

        if (equippedBarrel != null)
        {
            vertical *= equippedBarrel.verticalRecoilMultiplier;
            horizontal *= equippedBarrel.horizontalRecoilMultiplier;
        }

        return new Vector2(vertical, horizontal);
    }

    /// <summary>
    /// Get camera recoil multiplier
    /// </summary>
    public float GetCameraRecoilMultiplier()
    {
        if (equippedBarrel != null)
            return equippedBarrel.cameraRecoilMultiplier;
        return 1f;
    }

    /// <summary>
    /// Get muzzle effect settings from barrel attachment
    /// </summary>
    public void GetMuzzleEffectSettings(out int smokeParticles, out float smokeVelocity, out float flashIntensity)
    {
        if (equippedBarrel != null)
        {
            smokeParticles = equippedBarrel.smokeParticlesPerShot;
            smokeVelocity = equippedBarrel.smokeVelocityMultiplier;
            flashIntensity = equippedBarrel.flashIntensityMultiplier;
        }
        else
        {
            smokeParticles = 3;
            smokeVelocity = 1f;
            flashIntensity = 1f;
        }
    }

    /// <summary>
    /// Set the base weapon data (called by WeaponController on Initialize)
    /// </summary>
    public void SetBaseWeaponData(WeaponData data)
    {
        baseWeaponData = data;
        RecalculateWeaponStats();
    }

    /// <summary>
    /// Equip a new optic
    /// </summary>
    public void EquipOptic(ScopeData newOptic)
    {
        equippedOptic = newOptic;
        ApplyOptic();
        RecalculateWeaponStats();
    }

    /// <summary>
    /// Equip a new barrel attachment
    /// </summary>
    public void EquipBarrelAttachment(BarrelAttachmentData newBarrel)
    {
        equippedBarrel = newBarrel;
        ApplyBarrelAttachment();
        RecalculateWeaponStats();
    }

    /// <summary>
    /// Equip a new laser sight
    /// </summary>
    public void EquipLaser(LaserData newLaser)
    {
        equippedLaser = newLaser;
        ApplyLaserSight();
        RecalculateWeaponStats();
    }

    void OnDestroy()
    {
        // Cleanup spawned objects
        if (spawnedOpticModel != null) Destroy(spawnedOpticModel);
        if (spawnedBarrelModel != null) Destroy(spawnedBarrelModel);
        if (spawnedLaserObject != null) Destroy(spawnedLaserObject);
    }
}

/// <summary>
/// Helper class to store modified weapon stats
/// </summary>
[System.Serializable]
public class WeaponStats
{
    public float verticalRecoil;
    public float horizontalRecoil;
    public float cameraRecoil;
    public float kickback;
    public float range;
    public float adsSpeed;
    public float hipFireAccuracy;

    public WeaponStats(WeaponData baseData)
    {
        verticalRecoil = baseData.recoilPattern.x;
        horizontalRecoil = baseData.recoilPattern.y;
        cameraRecoil = baseData.cameraVerticalRecoil;
        kickback = 1f; // Default
        range = baseData.range;
        adsSpeed = baseData.adsSpeedOverride;
        hipFireAccuracy = baseData.accuracy;
    }
}
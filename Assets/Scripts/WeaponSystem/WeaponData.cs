using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum WeaponType { Pistol, Rifle, Shotgun, SMG, Sniper, LMG }

[System.Serializable]
public enum ShotgunSpreadPattern { Circular, Horizontal, Vertical, Random }

[System.Serializable]
public struct TransformOffset
{
    public Vector3 position;
    public Vector3 rotation;
    public TransformOffset(Vector3 pos, Vector3 rot) { position = pos; rotation = rot; }
}

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("=== BASIC INFO ===")]
    public string weaponName = "New Weapon";
    public WeaponType weaponType = WeaponType.Rifle;
    public GameObject weaponPrefab;

    [Header("=== STATS ===")]
    public float damage = 25f;
    public float fireRate = 600f;
    public float range = 100f;
    [Range(0f, 1f)] public float accuracy = 1f;

    [Header("=== BALLISTICS (Simple Values) ===")]
    [Tooltip("Enable realistic bullet drop and travel")]
    public bool useBallisticPhysics = false;

    [Tooltip("Bullet velocity in m/s (SMG: 250-350, Rifle: 500-700, Sniper: 800+)")]
    public float muzzleVelocity = 400f;

    [Tooltip("How far bullet travels perfectly straight before drop begins (meters)")]
    public float straightFlightDistance = 50f;

    [Tooltip("Bullet mass in kg - heavier = steeper arc after falloff (9mm: 0.008, 5.56: 0.004, .308: 0.010)")]
    public float bulletMass = 0.008f;

    [Tooltip("Gravity multiplier - how quickly bullet falls (1.0 = realistic, 2.0 = dramatic drop)")]
    public float bulletGravity = 1.5f;

    [Tooltip("Damage at max range as percentage of base damage (0.0 - 1.0)")]
    [Range(0f, 1f)]
    public float minDamageMultiplier = 0.3f;

    [Tooltip("Distance where damage starts falling off (meters)")]
    public float damageDropoffStart = 30f;

    [Tooltip("Number of simulation segments (higher = more accurate trajectory)")]
    [Range(10, 100)]
    public int trajectorySegments = 30;

    [Header("=== AMMO ===")]
    public int magazineSize = 30;
    public int maxReserveAmmo = 120;
    public float reloadTime = 2f;

    [Header("=== POSITIONING ===")]
    public TransformOffset hipFirePosition;
    public TransformOffset adsPosition;
    public TransformOffset sprintPosition;
    public Vector3 adsPositionOffset = Vector3.zero;

    [Header("=== ADS SETTINGS ===")]
    public float adsSpeedOverride = 8f;
    public float adsFOVOverride = 50f;
    public bool allowHoldBreath = true;

    [Header("=== RECOIL (Simple Pattern) ===")]
    [Tooltip("X = Vertical recoil (up/down), Y = Horizontal recoil (left/right)")]
    public Vector2 recoilPattern = new Vector2(3.0f, 0.3f);

    [Tooltip("General recoil multiplier")]
    public float recoilAmount = 1f;

    [Header("=== CAMERA RECOIL ===")]
    public float cameraVerticalRecoil = 0.5f;
    public float cameraHorizontalRecoil = 0.3f;
    public float cameraHorizontalVariance = 0.2f;
    [Range(0.5f, 10f)]
    public float recoilResistance = 2f;

    [Header("=== SHOTGUN SETTINGS ===")]
    public bool isShotgun = false;
    public bool enableShotgunSpread = true;
    public int pelletsPerShot = 8;
    public float spreadAngle = 10f;
    public ShotgunSpreadPattern spreadPattern = ShotgunSpreadPattern.Circular;

    [Header("=== SCOPES ===")]
    public List<ScopeData> availableScopes = new List<ScopeData>();
    public int defaultScopeIndex = 0;

    [Header("=== AUDIO ===")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    [Header("=== EFFECTS ===")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    public GameObject tracerPrefab;
    public GameObject casingPrefab;

    public enum BarrelAttachment { None, Suppressor, FlashHider, MuzzleBrake }

    [Header("=== BARREL ATTACHMENTS ===")]
    public BarrelAttachment barrelAttachment = BarrelAttachment.None;
    public GameObject muzzleSmokePrefab;
    public int smokeParticlesPerShot = 3;
    public float smokeVelocityMultiplier = 1f;

    // === HELPER METHODS ===

    public float CalculateDamageAtDistance(float distance)
    {
        if (distance <= damageDropoffStart)
            return damage;

        if (distance >= range)
            return damage * minDamageMultiplier;

        float falloffRange = range - damageDropoffStart;
        float distanceIntoFalloff = distance - damageDropoffStart;
        float falloffProgress = distanceIntoFalloff / falloffRange;

        float multiplier = Mathf.Lerp(1f, minDamageMultiplier, falloffProgress);
        return damage * multiplier;
    }

    public float GetEffectiveGravity()
    {
        float massInfluence = Mathf.Lerp(1.2f, 0.8f, bulletMass / 0.015f);
        return 9.81f * bulletGravity * massInfluence;
    }
}
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

    [Header("=== BALLISTICS ===")]
    [Tooltip("Enable realistic bullet drop and travel")]
    public bool useBallisticPhysics = false;

    [Tooltip("Bullet velocity in m/s (lower = more drop. SMG: 250-350, Rifle: 500-700, Sniper: 800+)")]
    public float bulletVelocity = 300f;

    [Tooltip("Gravity strength (higher = more drop. Earth: 9.81, Dramatic: 15-20)")]
    public float bulletGravity = 15f;

    [Tooltip("Number of curve segments (higher = more accurate, more expensive)")]
    [Range(5, 50)]
    public int trajectorySegments = 20;

    [Tooltip("Damage falloff curve (X=distance 0-1, Y=damage multiplier 0-1)")]
    public AnimationCurve damageFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0.5f);

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

    [Tooltip("General recoil multiplier (deprecated, use recoilPattern instead)")]
    public float recoilAmount = 1f;

    [Header("=== CAMERA RECOIL ===")]
    [Tooltip("How much camera rotates up per shot (degrees)")]
    public float cameraVerticalRecoil = 0.5f;

    [Tooltip("Random horizontal camera push (degrees)")]
    public float cameraHorizontalRecoil = 0.3f;

    [Tooltip("Random variance for camera horizontal")]
    public float cameraHorizontalVariance = 0.2f;

    [Tooltip("How much resistance/drag when recovering (lower = more resistance, harder to pull down)")]
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

    [Tooltip("Muzzle smoke effect (drag StandardMuzzleSmoke here)")]
    public GameObject muzzleSmokePrefab;

    [Tooltip("Smoke particles per shot")]
    public int smokeParticlesPerShot = 3;

    [Tooltip("Forward velocity multiplier based on attachment (Muzzle Brake = more)")]
    public float smokeVelocityMultiplier = 1f;

    // === LASER SIGHT moved to AttachmentManager + LaserData ===
    // Use AttachmentManager.equippedLaser instead
}
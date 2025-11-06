using UnityEngine;
using System.Collections.Generic;

public enum HandPreference
{
    Right,
    Left
}

/// <summary>
/// CLEAN VERSION - Weapon configuration scriptable object
/// Includes smoke system and procedural reload settings
/// NOW WITH: Laser alignment option for ADS
/// UPDATED WITH: ADS Offset System and Dual/Multi-Point Recoil
/// </summary>
[CreateAssetMenu(fileName = "New Weapon", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Identity")]
    public string weaponName = "New Weapon";
    public WeaponType weaponType = WeaponType.AssaultRifle;
    public GameObject weaponModelPrefab;

    [Header("Hand Preference")]
    [Tooltip("Right or Left handed weapon position")]
    public HandPreference handPreference = HandPreference.Right;

    [Header("Weapon Positioning - Right Hand (will mirror for left)")]
    [Tooltip("Where the weapon sits relative to camera (right-handed position)")]
    public Vector3 weaponHolderPosition = new Vector3(0.3f, -0.2f, 0.5f);
    [Tooltip("Rotation of the weapon holder")]
    public Vector3 weaponHolderRotation = Vector3.zero;

    [Header("Weapon Pivot Point")]
    [Tooltip("The pivot point for weapon sway and recoil (relative to weapon holder)")]
    public Vector3 pivotOffset = Vector3.zero;

    [Header("Attachment Points (Relative to Weapon Model Root)")]
    [Tooltip("Where bullets spawn from AND where suppressor attaches")]
    public TransformOffset muzzlePoint = new TransformOffset(new Vector3(0, 0.1f, 0.5f), Vector3.zero);

    [Tooltip("Where casings eject from - FOR X-AXIS CHARACTERS USE: X=forward, Y=up, Z=right")]
    public TransformOffset casingEjectionPoint = new TransformOffset(new Vector3(0.2f, 0.05f, 0.1f), new Vector3(0, 0, 90));

    [Tooltip("Where the laser sight originates - SEPARATE from muzzle (usually on rail/underbarrel)")]
    public TransformOffset laserOriginPoint = new TransformOffset(new Vector3(0.05f, -0.05f, 0.3f), Vector3.zero);

    [Header("Suppressor Settings")]
    [Tooltip("Offset from muzzle point where suppressor sits when attached")]
    public Vector3 suppressorOffset = new Vector3(0, 0, 0.05f);

    [Header("Stats")]
    public int magSize = 30;
    public int reserveAmmo = 90;
    public float fireRate = 0.1f;
    public float damage = 25f;
    public float range = 100f;
    public float bulletVelocity = 1000f;

    [Header("Recoil")]
    public float recoilX = 0.5f;
    public float recoilY = 2f;
    public float recoilSnapSpeed = 15f;
    public float recoilReturnSpeed = 8f;
    public float maxRecoilAccumulation = 10f;

    [Header("Weapon Visual Recoil")]
    public float weaponRecoilRotationMultiplier = 3f;
    public float weaponRecoilPositionKick = 0.15f;
    public float weaponRecoilReturnSpeed = 12f;

    [Header("Accuracy")]
    public float bulletSpreadAngle = 2f;
    public float aimSpreadMultiplier = 0.3f;
    public float weaponRotationAmount = 0.6f;
    public float weaponRotationSpeed = 8f;

    [Header("Visual")]
    public Color tracerColor = Color.yellow;
    public float tracerBulbSize = 0.15f;

    [Header("Shotgun Settings")]
    public int numberOfPellets = 1;
    public float pelletSpreadAngle = 10f;
    public ShotgunSpreadPattern spreadPattern = ShotgunSpreadPattern.Cone;

    [Header("Muzzle Smoke - Based on Barrel Attachment")]
    [Tooltip("Standard smoke for normal barrel")]
    public ParticleSystem standardSmokeEffect;

    [Tooltip("Minimal smoke for suppressor")]
    public ParticleSystem suppressedSmokeEffect;

    [Tooltip("Heavy smoke for compensator")]
    public ParticleSystem compensatorSmokeEffect;

    [Header("Attachments")]
    public bool hasLaserSight = false;
    [Tooltip("Laser aligns to crosshair when ADS")]
    public bool alignLaserToADS = true;
    public bool hasSuppressor = false;
    public GameObject suppressorModel;
    
    [Header("=== SCOPES ===")]
    public List<ScopeData> availableScopes = new List<ScopeData>();
    public int defaultScopeIndex = 0;
    public string scopeMountName = "ScopeMount";

    [Header("Casing Ejection - UNIQUE PER WEAPON")]
    [Tooltip("Assign a UNIQUE casing particle system prefab for this weapon (will be instantiated)")]
    public ParticleSystem casingPrefab;
    [Tooltip("Casing size multiplier (1.0 = normal, 1.5 = bigger shells)")]
    public float casingSize = 1.0f;
    [Tooltip("Ejection force (pistol: 3-4, rifle: 5-6, shotgun: 4-5)")]
    public float casingEjectionForce = 5f;

    [Header("Procedural Reload Animation")]
    [Tooltip("How long the reload takes (seconds)")]
    public float reloadDuration = 2.0f;

    [Tooltip("Where weapon moves to during reload (relative to original position)")]
    public Vector3 reloadPositionOffset = new Vector3(0, -0.2f, -0.1f);

    [Tooltip("How weapon rotates during reload (euler angles)")]
    public Vector3 reloadRotationOffset = new Vector3(30, 0, -20);

    [Tooltip("Animation curve for smooth motion")]
    public AnimationCurve reloadAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip suppressedShootSound;

    [Header("=== NEW: ADS Offset System ===")]
    [Tooltip("Offset added to CharacterAnimatorController's base ADS position for this weapon")]
    public Vector3 adsPositionOffset = Vector3.zero;
    [Tooltip("Override ADS speed for this weapon (0 = use controller default)")]
    public float adsSpeedOverride = 0f;
    [Tooltip("Override ADS FOV for this weapon (0 = use controller default)")]
    public float adsFOVOverride = 0f;
    [Tooltip("Can this weapon use hold breath stabilization? (Disable for heavy weapons like LMGs)")]
    public bool allowHoldBreath = true;

    [Header("=== NEW: Dual/Multi-Point Recoil System ===")]
    [Tooltip("List of recoil points for more realistic weapon recoil behavior")]
    public List<RecoilPoint> recoilPoints = new List<RecoilPoint>();

    /// <summary>
    /// Helper method to create a transform with offset data
    /// </summary>
    public Transform CreateAttachmentPoint(Transform parent, TransformOffset offset, string name)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent);
        point.transform.localPosition = offset.position;
        point.transform.localRotation = Quaternion.Euler(offset.rotation);
        return point.transform;
    }
}

/// <summary>
/// Recoil point for dual/multi-point recoil system
/// </summary>
[System.Serializable]
public class RecoilPoint
{
    [Tooltip("Name for organization (e.g., 'Barrel', 'Stock', 'Grip')")]
    public string pointName = "Recoil Point";

    [Tooltip("Local position offset from weapon pivot")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("How much this point contributes to recoil (0-1)")]
    [Range(0f, 1f)]
    public float influence = 1f;

    [Tooltip("Rotation multiplier per axis:\nX = Pitch (up/down kick)\nY = Yaw (left/right)\nZ = Roll (kickback/forward)")]
    public Vector3 rotationMultiplier = Vector3.one;

    [Tooltip("Visual gizmo color in editor")]
    public Color gizmoColor = Color.red;

    public RecoilPoint()
    {
        pointName = "Recoil Point";
        positionOffset = Vector3.zero;
        influence = 1f;
        rotationMultiplier = Vector3.one;
        gizmoColor = Color.red;
    }

    public RecoilPoint(string name, Vector3 offset, float influence = 1f)
    {
        pointName = name;
        positionOffset = offset;
        this.influence = influence;
        rotationMultiplier = Vector3.one;
        gizmoColor = Color.red;
    }
}
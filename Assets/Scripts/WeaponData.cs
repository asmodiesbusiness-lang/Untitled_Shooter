using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    SMG,
    Sniper,
    LMG
}

[System.Serializable]
public enum ShotgunSpreadPattern
{
    Circular,
    Horizontal,
    Vertical,
    Random
}

[System.Serializable]
public struct TransformOffset
{
    public Vector3 position;
    public Vector3 rotation;

    public TransformOffset(Vector3 pos, Vector3 rot)
    {
        position = pos;
        rotation = rot;
    }
}

[System.Serializable]
public class RecoilPoint
{
    [Header("Recoil Point Settings")]
    public string pointName = "RecoilPoint";
    public Transform transform;
    public Vector3 positionOffset = Vector3.zero;

    [Header("Influence Settings")]
    public float influence = 1f;  // General influence (for backward compatibility)
    public float rotationInfluence = 1f;
    public float positionInfluence = 1f;
    public float rotationMultiplier = 1f;

    [Header("Debug")]
    public Color gizmoColor = Color.red;

    // Constructor for initialization
    public RecoilPoint()
    {
        pointName = "New Recoil Point";
        transform = null;
        positionOffset = Vector3.zero;
        influence = 1f;
        rotationInfluence = 1f;
        positionInfluence = 1f;
        rotationMultiplier = 1f;
        gizmoColor = Color.red;
    }
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
    public float accuracy = 1f;

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

    [Header("=== RECOIL ===")]
    public float recoilAmount = 1f;
    public Vector2 recoilPattern = new Vector2(2f, 0.5f);
    public List<RecoilPoint> recoilPoints = new List<RecoilPoint>();

    [Header("=== SHOTGUN SETTINGS ===")]
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
}
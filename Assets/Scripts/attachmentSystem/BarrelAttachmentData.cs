using UnityEngine;

[CreateAssetMenu(fileName = "New Barrel Attachment", menuName = "Weapons/Attachments/Barrel Attachment")]
public class BarrelAttachmentData : ScriptableObject
{
    [Header("=== BASIC INFO ===")]
    public string attachmentName = "Suppressor";
    public Sprite attachmentIcon;
    
    public enum BarrelType { None, Suppressor, FlashHider, MuzzleBrake, Compensator }
    public BarrelType barrelType = BarrelType.None;

    [Header("=== VISUAL MODEL ===")]
    [Tooltip("3D model of the attachment (optional)")]
    public GameObject attachmentModelPrefab;
    
    [Tooltip("Position offset from muzzle point")]
    public Vector3 modelPosition = Vector3.zero;
    
    [Tooltip("Rotation offset")]
    public Vector3 modelRotation = Vector3.zero;

    [Header("=== MUZZLE EFFECTS ===")]
    [Tooltip("Muzzle flash intensity multiplier (suppressor = 0.3, brake = 1.5)")]
    [Range(0f, 2f)]
    public float flashIntensityMultiplier = 1f;
    
    [Tooltip("Muzzle smoke amount (particles per shot)")]
    [Range(0, 10)]
    public int smokeParticlesPerShot = 3;
    
    [Tooltip("Smoke velocity multiplier (brake = more forward velocity)")]
    [Range(0f, 3f)]
    public float smokeVelocityMultiplier = 1f;
    
    [Tooltip("Smoke spread angle (brake = tighter, forward)")]
    [Range(0f, 45f)]
    public float smokeConeAngle = 15f;

    [Header("=== RECOIL MODIFIERS ===")]
    [Tooltip("Vertical recoil multiplier (compensator reduces vertical)")]
    [Range(0.5f, 1.5f)]
    public float verticalRecoilMultiplier = 1f;
    
    [Tooltip("Horizontal recoil multiplier (compensator increases horizontal)")]
    [Range(0.5f, 1.5f)]
    public float horizontalRecoilMultiplier = 1f;
    
    [Tooltip("Camera recoil multiplier (less camera kick)")]
    [Range(0.5f, 1.5f)]
    public float cameraRecoilMultiplier = 1f;
    
    [Tooltip("Weapon kickback distance multiplier")]
    [Range(0.5f, 1.5f)]
    public float kickbackMultiplier = 1f;

    [Header("=== SOUND MODIFIERS ===")]
    [Tooltip("Suppressor reduces volume, brake increases")]
    [Range(0.3f, 1.5f)]
    public float soundVolumeMultiplier = 1f;
    
    [Tooltip("Pitch shift (suppressor = lower pitch)")]
    [Range(0.8f, 1.2f)]
    public float soundPitchMultiplier = 1f;
    
    [Tooltip("Custom fire sound (optional - overrides weapon sound)")]
    public AudioClip customFireSound;

    [Header("=== GAMEPLAY MODIFIERS ===")]
    [Tooltip("Range multiplier (suppressor reduces slightly)")]
    [Range(0.8f, 1.2f)]
    public float rangeMultiplier = 1f;
    
    [Tooltip("Velocity multiplier (affects bullet speed)")]
    [Range(0.9f, 1.1f)]
    public float velocityMultiplier = 1f;
    
    [Tooltip("ADS speed multiplier (heavier attachments slow ADS)")]
    [Range(0.9f, 1.1f)]
    public float adsSpeedMultiplier = 1f;

    [Header("=== ERGONOMICS ===")]
    [Tooltip("Adds weight (affects weapon handling)")]
    [Range(0f, 2f)]
    public float addedWeight = 0f;
}

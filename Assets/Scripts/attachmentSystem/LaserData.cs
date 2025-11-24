using UnityEngine;

[CreateAssetMenu(fileName = "New Laser", menuName = "Weapons/Attachments/Laser Sight")]
public class LaserData : ScriptableObject
{
    [Header("=== BASIC INFO ===")]
    public string laserName = "Red Laser";
    public Sprite laserIcon;

    [Header("=== VISUAL SETTINGS ===")]
    public Color laserColor = Color.red;
    public float laserWidth = 0.002f;
    public float dotSize = 0.02f;
    public float maxDistance = 50f;
    
    [Header("=== MOUNT POSITION ===")]
    [Tooltip("Position offset from weapon root")]
    public Vector3 mountPosition = new Vector3(0, -0.02f, 0.1f);
    
    [Tooltip("Rotation offset")]
    public Vector3 mountRotation = Vector3.zero;
    
    [Header("=== LIGHT SETTINGS ===")]
    [Tooltip("Enable point light at laser dot")]
    public bool hasPointLight = true;
    public float lightIntensity = 2f;
    public float lightRange = 0.5f;

    [Header("=== STAT MODIFIERS ===")]
    [Tooltip("Reduces hip fire spread")]
    [Range(0f, 1f)]
    public float hipFireAccuracyBonus = 0.1f;
    
    [Tooltip("Slight ADS speed penalty")]
    public float adsSpeedMultiplier = 0.98f;
}

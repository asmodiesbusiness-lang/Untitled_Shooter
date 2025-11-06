using UnityEngine;

[CreateAssetMenu(fileName = "New Scope", menuName = "Weapon System/Scope Data")]
public class ScopeData : ScriptableObject
{
    [Header("=== SCOPE INFO ===")]
    public string scopeName = "Red Dot";
    public ScopeType scopeType = ScopeType.RedDot;
    
    [Header("=== ZOOM SETTINGS (Option B - Stepped Zoom) ===")]
    [Tooltip("Starting zoom level (1x, 2x, 4x, etc)")]
    public float minZoomLevel = 1f;
    
    [Tooltip("Maximum zoom level")]
    public float maxZoomLevel = 1f;
    
    [Tooltip("How much zoom changes per scroll step")]
    public float zoomStepSize = 0.5f;
    
    [Tooltip("Enable scroll wheel zoom (for variable scopes)")]
    public bool allowScrollZoom = false;
    
    [Header("=== FOV MAPPING ===")]
    [Tooltip("FOV at minimum zoom (higher FOV = less zoomed)")]
    public float fovAtMinZoom = 60f;
    
    [Tooltip("FOV at maximum zoom (lower FOV = more zoomed)")]
    public float fovAtMaxZoom = 20f;
    
    [Tooltip("How fast FOV transitions")]
    public float fovTransitionSpeed = 10f;
    
    [Header("=== SCOPE 3D MODEL ===")]
    [Tooltip("Optional 3D scope model prefab")]
    public GameObject scopeModelPrefab;
    
    [Tooltip("Position offset from weapon mount point")]
    public Vector3 modelPositionOffset = Vector3.zero;
    
    [Tooltip("Rotation offset")]
    public Vector3 modelRotationOffset = Vector3.zero;
    
    [Header("=== RETICLE (2D Overlay) ===")]
    [Tooltip("Reticle texture for 2D overlay")]
    public Texture2D reticleTexture;
    
    [Tooltip("Reticle color tint")]
    public Color reticleColor = new Color(1f, 0f, 0f, 0.8f);
    
    [Tooltip("Reticle size in pixels")]
    public float reticleSize = 32f;
    
    [Tooltip("Show reticle only when ADS")]
    public bool onlyShowWhenADS = true;
    
    [Header("=== ADS BEHAVIOR ===")]
    [Tooltip("Camera position offset when ADS")]
    public Vector3 adsCameraPosition = new Vector3(0, -0.05f, -0.3f);
    
    [Tooltip("ADS transition speed")]
    public float adsTransitionSpeed = 8f;
    
    [Tooltip("Movement speed multiplier when ADS")]
    [Range(0.3f, 1f)]
    public float adsMovementMultiplier = 0.7f;
    
    [Header("=== AUDIO ===")]
    public AudioClip zoomInSound;
    public AudioClip zoomOutSound;
    public AudioClip scrollZoomSound;
}

// Add this enum if you don't already have it
public enum ScopeType
{
    IronSights,
    RedDot,
    Holographic,
    ACOG_2x,
    ACOG_4x,
    Magnifier_3x,
    Sniper_6x,
    Sniper_8x,
    Sniper_12x,
    Variable_3_9x,
    Variable_4_12x,
    Thermal,
    NightVision
}

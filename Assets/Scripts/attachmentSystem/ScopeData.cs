using UnityEngine;

[CreateAssetMenu(fileName = "New Scope", menuName = "Weapons/Scope Data")]
public class ScopeData : ScriptableObject
{
    [Header("=== BASIC INFO ===")]
    public string scopeName = "Red Dot";
    public GameObject scopeModelPrefab;

    [Header("=== POSITIONING ===")]
    public Vector3 modelPositionOffset = Vector3.zero;
    public Vector3 modelRotationOffset = Vector3.zero;

    [Header("=== ZOOM ===")]
    public float minZoomLevel = 1.0f;
    public float maxZoomLevel = 4.0f;
    public float zoomStepSize = 0.5f;
    public bool allowScrollZoom = true;

    [Header("=== FOV ===")]
    public float fovAtMinZoom = 60f;
    public float fovAtMaxZoom = 20f;
    public float fovTransitionSpeed = 10f;

    [Header("=== ADS ===")]
    public Vector3 adsCameraPosition = new Vector3(0, 0, -0.2f);
    public float adsTransitionSpeed = 8f;
    public float adsMovementMultiplier = 0.5f;

    [Header("=== RETICLE ===")]
    public Texture2D reticleTexture;
    public Color reticleColor = Color.red;
    public float reticleSize = 32f;
    public bool onlyShowWhenADS = true;

    [Header("=== AUDIO ===")]
    public AudioClip zoomInSound;
    public AudioClip zoomOutSound;
    public AudioClip scrollZoomSound;
}
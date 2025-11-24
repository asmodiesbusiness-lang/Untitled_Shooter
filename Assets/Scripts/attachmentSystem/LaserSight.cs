using UnityEngine;

/// <summary>
/// Realistic laser sight with day/night visibility adaptation
/// Barely visible in bright areas, clearly visible in dark areas
/// </summary>
public class LaserSight : MonoBehaviour
{
    [Header("=== LASER SETTINGS ===")]
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserMaxDistance = 50f;
    [SerializeField] private float laserWidth = 0.002f;
    [SerializeField] private float dotSize = 0.02f;

    [Header("=== MOUNT POSITION ===")]
    [SerializeField] private Vector3 mountOffset = new Vector3(0, -0.02f, 0.1f);
    [SerializeField] private Vector3 mountRotation = Vector3.zero;

    [Header("=== VISIBILITY (Realistic) ===")]
    [Tooltip("Maximum alpha in pitch black")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlphaInDark = 0.8f;

    [Tooltip("Minimum alpha in bright daylight")]
    [Range(0f, 0.3f)]
    [SerializeField] private float minAlphaInLight = 0.05f;

    [Tooltip("How bright environment affects visibility")]
    [SerializeField] private float environmentSensitivity = 1.5f;

    [Tooltip("Distance fade start (laser gets dimmer far away)")]
    [SerializeField] private float fadeStartDistance = 20f;

    [Header("=== LIGHT SETTINGS ===")]
    [SerializeField] private bool hasPointLight = true;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightRange = 0.5f;

    private LineRenderer laserLine;
    private GameObject laserDot;
    private Light laserLight;
    private Material laserMaterial;
    private Material dotMaterial;

    // Cached values
    private float currentAlpha = 1f;
    private float targetAlpha = 1f;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        SetupLaser();

        // Apply mount offset from inspector
        transform.localPosition = mountOffset;
        transform.localEulerAngles = mountRotation;
    }

    void SetupLaser()
    {
        // Create laser line with custom material
        laserLine = gameObject.AddComponent<LineRenderer>();
        laserMaterial = new Material(Shader.Find("Sprites/Default")); // Additive-like shader
        laserMaterial.color = laserColor;

        laserLine.material = laserMaterial;
        laserLine.startWidth = laserWidth;
        laserLine.endWidth = laserWidth;
        laserLine.positionCount = 2;
        laserLine.useWorldSpace = true;
        laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        laserLine.receiveShadows = false;

        // Create laser dot
        laserDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        laserDot.name = "LaserDot";
        laserDot.transform.SetParent(transform);
        laserDot.transform.localScale = Vector3.one * dotSize;
        Destroy(laserDot.GetComponent<Collider>());

        Renderer dotRenderer = laserDot.GetComponent<Renderer>();
        dotMaterial = new Material(Shader.Find("Unlit/Color"));
        dotMaterial.color = laserColor;
        dotRenderer.material = dotMaterial;
        dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dotRenderer.receiveShadows = false;

        // Create point light at dot
        if (hasPointLight)
        {
            laserLight = laserDot.AddComponent<Light>();
            laserLight.type = LightType.Point;
            laserLight.color = laserColor;
            laserLight.range = lightRange;
            laserLight.intensity = lightIntensity;
            // Lights don't cast shadows by default in Unity, so we're good
        }
    }

    void Update()
    {
        UpdateLaserVisibility();
        UpdateLaser();
    }

    void UpdateLaserVisibility()
    {
        // Calculate ambient brightness (simple approximation)
        float ambientBrightness = CalculateAmbientBrightness();

        // Map brightness to alpha (bright = barely visible, dark = very visible)
        targetAlpha = Mathf.Lerp(maxAlphaInDark, minAlphaInLight, ambientBrightness);

        // Smooth transition
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * 5f);
    }

    float CalculateAmbientBrightness()
    {
        // Method 1: Use ambient light intensity
        float ambientIntensity = RenderSettings.ambientIntensity;

        // Method 2: Add sun/directional light contribution if exists
        Light sun = RenderSettings.sun;
        if (sun != null && sun.type == LightType.Directional)
        {
            ambientIntensity += sun.intensity * 0.5f; // Sun contributes to brightness
        }

        // Clamp and apply sensitivity
        ambientIntensity = Mathf.Clamp01(ambientIntensity * environmentSensitivity);

        return ambientIntensity;
    }

    void UpdateLaser()
    {
        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;

        // Raycast to find laser end point
        RaycastHit hit;
        Vector3 endPos;
        float distance;

        int layerMask = ~LayerMask.GetMask("Player"); // Ignore player

        if (Physics.Raycast(startPos, direction, out hit, laserMaxDistance, layerMask))
        {
            endPos = hit.point;
            distance = hit.distance;
        }
        else
        {
            endPos = startPos + direction * laserMaxDistance;
            distance = laserMaxDistance;
        }

        // Calculate distance-based fade
        float distanceFade = 1f;
        if (distance > fadeStartDistance)
        {
            float fadeProgress = (distance - fadeStartDistance) / (laserMaxDistance - fadeStartDistance);
            distanceFade = Mathf.Lerp(1f, 0.3f, fadeProgress); // Fade to 30% at max distance
        }

        // Apply alpha to line and dot
        float finalAlpha = currentAlpha * distanceFade;
        ApplyAlpha(finalAlpha);

        // Update line positions
        laserLine.SetPosition(0, startPos);
        laserLine.SetPosition(1, endPos);

        // Update dot position
        laserDot.transform.position = endPos;

        // Update light intensity based on visibility
        if (laserLight != null)
        {
            laserLight.intensity = lightIntensity * finalAlpha;
        }
    }

    void ApplyAlpha(float alpha)
    {
        // Update line color
        Color lineColor = laserColor;
        lineColor.a = alpha;
        laserMaterial.color = lineColor;

        // Update dot color
        Color dotColor = laserColor;
        dotColor.a = Mathf.Min(alpha * 2f, 1f); // Dot is slightly brighter than line
        dotMaterial.color = dotColor;
    }

    public void SetMountOffset(Vector3 position, Vector3 rotation)
    {
        mountOffset = position;
        mountRotation = rotation;

        transform.localPosition = mountOffset;
        transform.localEulerAngles = mountRotation;
    }

    public void ConfigureFromData(LaserData data)
    {
        // Apply all settings from LaserData
        laserColor = data.laserColor;
        laserMaxDistance = data.maxDistance;
        laserWidth = data.laserWidth;
        dotSize = data.dotSize;
        mountOffset = data.mountPosition;
        mountRotation = data.mountRotation;
        hasPointLight = data.hasPointLight;
        lightIntensity = data.lightIntensity;
        lightRange = data.lightRange;

        // Apply mount position
        transform.localPosition = mountOffset;
        transform.localEulerAngles = mountRotation;

        // If already setup, update materials
        if (laserMaterial != null)
        {
            Color c = laserColor;
            c.a = currentAlpha;
            laserMaterial.color = c;
        }
        if (dotMaterial != null)
        {
            Color c = laserColor;
            c.a = currentAlpha;
            dotMaterial.color = c;
        }
        if (laserLight != null)
        {
            laserLight.color = laserColor;
            laserLight.intensity = lightIntensity;
            laserLight.range = lightRange;
        }
    }

    public void SetLaserColor(Color color)
    {
        laserColor = color;
        if (laserMaterial != null)
        {
            Color c = color;
            c.a = currentAlpha;
            laserMaterial.color = c;
        }
        if (dotMaterial != null)
        {
            Color c = color;
            c.a = currentAlpha;
            dotMaterial.color = c;
        }
        if (laserLight != null)
            laserLight.color = color;
    }

    public void SetEnabled(bool enabled)
    {
        if (laserLine != null) laserLine.enabled = enabled;
        if (laserDot != null) laserDot.SetActive(enabled);
        if (laserLight != null) laserLight.enabled = enabled;
    }

    void OnDestroy()
    {
        if (laserDot != null)
            Destroy(laserDot);
    }

    // Debug info
    void OnGUI()
    {
        if (Debug.isDebugBuild)
        {
            float brightness = CalculateAmbientBrightness();
            GUI.Label(new Rect(10, 150, 300, 20), $"Laser Alpha: {currentAlpha:F2} | Brightness: {brightness:F2}");
        }
    }
}
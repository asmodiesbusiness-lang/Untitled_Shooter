using UnityEngine;

/// <summary>
/// Creates a simple red dot reticle that's always visible
/// Attach this to your scope or weapon
/// </summary>
public class RedDotReticle : MonoBehaviour
{
    [Header("=== RETICLE SETTINGS ===")]
    [Tooltip("Color of the reticle dot")]
    [SerializeField] private Color reticleColor = Color.red;

    [Tooltip("Size of the dot")]
    [SerializeField] private float dotSize = 0.002f;

    [Tooltip("Position offset from this GameObject")]
    [SerializeField] private Vector3 reticleOffset = new Vector3(0, 0, 0.05f);

    [Tooltip("Enable emission glow")]
    [SerializeField] private bool enableEmission = true;

    [Tooltip("Emission intensity")]
    [SerializeField] private float emissionIntensity = 2f;

    private GameObject reticleDot;
    private Material reticleMaterial;

    void Start()
    {
        CreateReticle();
    }

    void CreateReticle()
    {
        // Create reticle dot as sphere
        reticleDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        reticleDot.name = "ReticleDot";
        reticleDot.transform.SetParent(transform);
        reticleDot.transform.localPosition = reticleOffset;
        reticleDot.transform.localScale = Vector3.one * dotSize;

        // Remove collider
        Destroy(reticleDot.GetComponent<Collider>());

        // Create simple unlit material (no weird emission colors)
        reticleMaterial = new Material(Shader.Find("Unlit/Color"));
        reticleMaterial.color = reticleColor;

        // Apply material
        reticleDot.GetComponent<Renderer>().material = reticleMaterial;
        reticleDot.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        reticleDot.GetComponent<Renderer>().receiveShadows = false;

        // Add point light for glow effect
        if (enableEmission)
        {
            Light dotLight = reticleDot.AddComponent<Light>();
            dotLight.type = LightType.Point;
            dotLight.color = reticleColor;
            dotLight.range = 0.3f;
            dotLight.intensity = emissionIntensity * 0.5f;
        }

        Debug.Log("[RedDotReticle] Reticle created at " + reticleDot.transform.position);
    }

    public void SetReticleColor(Color color)
    {
        reticleColor = color;
        if (reticleMaterial != null)
        {
            reticleMaterial.color = color;
        }

        // Update light color if exists
        Light dotLight = reticleDot?.GetComponent<Light>();
        if (dotLight != null)
        {
            dotLight.color = color;
        }
    }

    public void SetReticleSize(float size)
    {
        dotSize = size;
        if (reticleDot != null)
        {
            reticleDot.transform.localScale = Vector3.one * size;
        }
    }

    void OnDestroy()
    {
        if (reticleDot != null)
            Destroy(reticleDot);
    }

    // Draw gizmo to show reticle position in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = reticleColor;
        Vector3 worldPos = transform.TransformPoint(reticleOffset);
        Gizmos.DrawWireSphere(worldPos, dotSize * 2f);
    }
}
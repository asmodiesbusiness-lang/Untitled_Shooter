using UnityEngine;

/// <summary>
/// World of Dogs style weapon tracking - weapon follows cursor within a bounding box
/// Weapon rotation is clamped and creates indie FPS aiming feel
/// Attach to Main Camera (same as WeaponManager)
/// </summary>
[DefaultExecutionOrder(100)]
public class WeaponBoundingBox : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private CharacterAnimatorController characterAnimator;
    
    [Header("=== BOUNDING BOX SETTINGS ===")]
    [Tooltip("Enable weapon tracking")]
    [SerializeField] private bool enableTracking = true;
    
    [Tooltip("Maximum weapon rotation in degrees")]
    [SerializeField] private float maxRotationAngle = 15f;
    
    [Tooltip("Bounding box size (0-1, percentage of screen)")]
    [SerializeField] private Vector2 boundingBoxSize = new Vector2(0.3f, 0.3f);
    
    [Tooltip("How smoothly weapon follows cursor")]
    [SerializeField] private float followSpeed = 8f;
    
    [Tooltip("Disable tracking when ADS")]
    [SerializeField] private bool disableOnADS = true;
    
    [Header("=== DEBUG ===")]
    [SerializeField] private bool showBoundingBox = true;
    [SerializeField] private Color boxColor = Color.yellow;
    
    private Vector3 currentRotation = Vector3.zero;
    private Vector3 targetRotation = Vector3.zero;
    
    void Start()
    {
        if (characterAnimator == null)
            characterAnimator = FindObjectOfType<CharacterAnimatorController>();
            
        if (weaponHolder == null)
        {
            GameObject holderObj = GameObject.Find("WeaponHolder");
            if (holderObj != null)
                weaponHolder = holderObj.transform;
        }
        
        Debug.Log($"[WeaponBoundingBox] Initialized - Max Angle: {maxRotationAngle}°, Box Size: {boundingBoxSize}");
    }
    
    void LateUpdate()
    {
        if (!enableTracking || weaponHolder == null || weaponHolder.childCount == 0)
            return;
        
        bool isAiming = characterAnimator != null && characterAnimator.IsAiming();
        
        if (disableOnADS && isAiming)
        {
            // Center weapon when ADS
            targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, Time.deltaTime * followSpeed);
        }
        else
        {
            // Calculate target rotation based on cursor position in bounding box
            Vector3 mousePos = Input.mousePosition;
            
            // Get screen center
            float screenCenterX = Screen.width / 2f;
            float screenCenterY = Screen.height / 2f;
            
            // Calculate bounding box in screen pixels
            float boxHalfWidth = (Screen.width * boundingBoxSize.x) / 2f;
            float boxHalfHeight = (Screen.height * boundingBoxSize.y) / 2f;
            
            // Clamp mouse position to bounding box
            float clampedX = Mathf.Clamp(mousePos.x, screenCenterX - boxHalfWidth, screenCenterX + boxHalfWidth);
            float clampedY = Mathf.Clamp(mousePos.y, screenCenterY - boxHalfHeight, screenCenterY + boxHalfHeight);
            
            // Normalize to -1 to 1 range within the box
            float normalizedX = (clampedX - screenCenterX) / boxHalfWidth;
            float normalizedY = (clampedY - screenCenterY) / boxHalfHeight;
            
            // Convert to rotation angles
            targetRotation = new Vector3(
                -normalizedY * maxRotationAngle,  // Pitch (up/down)
                normalizedX * maxRotationAngle,   // Yaw (left/right)
                0f
            );
        }
        
        // Smooth interpolation
        currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * followSpeed);
        
        // Apply rotation to weapon
        Transform weapon = weaponHolder.GetChild(0);
        Quaternion weaponRotation = Quaternion.Euler(currentRotation);
        
        // Apply ON TOP of existing rotation from CharacterAnimatorController
        weapon.localRotation = weapon.localRotation * weaponRotation;
    }
    
    void OnGUI()
    {
        if (!showBoundingBox || !enableTracking) return;
        
        // Draw bounding box
        float screenCenterX = Screen.width / 2f;
        float screenCenterY = Screen.height / 2f;
        
        float boxHalfWidth = (Screen.width * boundingBoxSize.x) / 2f;
        float boxHalfHeight = (Screen.height * boundingBoxSize.y) / 2f;
        
        // Convert to GUI coordinates (Y is inverted)
        float guiCenterY = Screen.height - screenCenterY;
        
        Rect boxRect = new Rect(
            screenCenterX - boxHalfWidth,
            guiCenterY - boxHalfHeight,
            boxHalfWidth * 2f,
            boxHalfHeight * 2f
        );
        
        // Draw box outline
        GUI.color = boxColor;
        DrawRectOutline(boxRect, 2);
        
        // Draw center crosshair
        float crosshairSize = 10f;
        GUI.DrawTexture(new Rect(screenCenterX - 1, guiCenterY - crosshairSize, 2, crosshairSize * 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screenCenterX - crosshairSize, guiCenterY - 1, crosshairSize * 2, 2), Texture2D.whiteTexture);
        
        GUI.color = Color.white;
    }
    
    void DrawRectOutline(Rect rect, float thickness)
    {
        // Top
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        // Bottom
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        // Left
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        // Right
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }
    
    // Public API
    public void SetEnabled(bool enabled)
    {
        enableTracking = enabled;
    }
    
    public void SetMaxAngle(float angle)
    {
        maxRotationAngle = angle;
    }
    
    public void SetBoundingBoxSize(Vector2 size)
    {
        boundingBoxSize = size;
    }
}

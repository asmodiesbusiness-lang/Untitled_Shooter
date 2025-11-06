using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// FIXED VERSION - Auto-creates missing ScopeMount
/// </summary>
public class ScopeManager : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Transform weaponHolder;

    [Header("=== INPUT ===")]
    [SerializeField] private KeyCode adsKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode cycleScopeKey = KeyCode.B;
    [SerializeField] private string scrollAxisName = "Mouse ScrollWheel";

    [Header("=== STATE ===")]
    private WeaponData currentWeapon;
    private List<ScopeData> availableScopes;
    private int currentScopeIndex = 0;
    private ScopeData currentScope;
    private GameObject spawnedScopeModel;
    private Transform scopeMount;

    [Header("=== ZOOM STATE ===")]
    private float currentZoomLevel = 1f;
    private float targetFOV;
    private float baseFOV = 60f;
    private bool isADS = false;

    [Header("=== CAMERA STATE ===")]
    private Vector3 baseCameraPosition;
    private Vector3 targetCameraPosition;

    [Header("=== SETTINGS ===")]
    [SerializeField] private bool enableScopeModels = true;
    [SerializeField] private bool enableReticleOverlay = true;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showZoomSteps = false;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (weaponManager == null)
            weaponManager = GetComponent<WeaponManager>();

        baseFOV = playerCamera.fieldOfView;
        targetFOV = baseFOV;
        baseCameraPosition = playerCamera.transform.localPosition;
        targetCameraPosition = baseCameraPosition;

        if (weaponManager != null)
            OnWeaponChanged(weaponManager.GetCurrentWeaponData());
    }

    void Update()
    {
        HandleInput();
        UpdateFOV();
        UpdateCameraPosition();
    }

    void OnGUI()
    {
        if (showDebugInfo)
            DrawDebugInfo();

        if (enableReticleOverlay && isADS && currentScope != null)
            DrawReticle();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(adsKey))
        {
            EnterADS();
        }
        else if (Input.GetKeyUp(adsKey))
        {
            ExitADS();
        }

        if (isADS && currentScope != null && currentScope.allowScrollZoom)
        {
            float scroll = Input.GetAxis(scrollAxisName);
            if (Mathf.Abs(scroll) > 0.01f)
            {
                StepZoom(scroll > 0 ? 1 : -1);
            }
        }

        if (Input.GetKeyDown(cycleScopeKey))
        {
            CycleScope();
        }
    }

    void EnterADS()
    {
        if (currentScope == null) return;

        isADS = true;
        currentZoomLevel = currentScope.minZoomLevel;
        targetFOV = CalculateFOVForZoom(currentZoomLevel);
        targetCameraPosition = baseCameraPosition + currentScope.adsCameraPosition;

        if (currentScope.zoomInSound != null)
            AudioSource.PlayClipAtPoint(currentScope.zoomInSound, playerCamera.transform.position, 0.5f);

        if (showDebugInfo)
            Debug.Log("[Scope] ADS: " + currentScope.scopeName + " @ " + currentZoomLevel + "x");
    }

    void ExitADS()
    {
        isADS = false;
        currentZoomLevel = 1f;
        targetFOV = baseFOV;
        targetCameraPosition = baseCameraPosition;

        if (currentScope != null && currentScope.zoomOutSound != null)
            AudioSource.PlayClipAtPoint(currentScope.zoomOutSound, playerCamera.transform.position, 0.5f);
    }

    void StepZoom(int direction)
    {
        if (currentScope == null) return;

        float newZoom = currentZoomLevel + (direction * currentScope.zoomStepSize);
        newZoom = Mathf.Clamp(newZoom, currentScope.minZoomLevel, currentScope.maxZoomLevel);

        if (Mathf.Abs(newZoom - currentZoomLevel) > 0.01f)
        {
            currentZoomLevel = newZoom;
            targetFOV = CalculateFOVForZoom(currentZoomLevel);

            if (currentScope.scrollZoomSound != null)
                AudioSource.PlayClipAtPoint(currentScope.scrollZoomSound, playerCamera.transform.position, 0.3f);

            if (showZoomSteps)
                Debug.Log("[Scope] Zoom: " + currentZoomLevel.ToString("F1") + "x (FOV: " + targetFOV.ToString("F1") + "deg)");
        }
    }

    float CalculateFOVForZoom(float zoomLevel)
    {
        if (currentScope == null) return baseFOV;

        float t = Mathf.InverseLerp(currentScope.minZoomLevel, currentScope.maxZoomLevel, zoomLevel);
        return Mathf.Lerp(currentScope.fovAtMinZoom, currentScope.fovAtMaxZoom, t);
    }

    void CycleScope()
    {
        if (availableScopes == null || availableScopes.Count == 0) return;

        currentScopeIndex = (currentScopeIndex + 1) % availableScopes.Count;
        EquipScope(currentScopeIndex);

        Debug.Log("[Scope] Switched to: " + currentScope.scopeName);
    }

    void EquipScope(int index)
    {
        if (availableScopes == null || index < 0 || index >= availableScopes.Count)
            return;

        currentScopeIndex = index;
        currentScope = availableScopes[index];
        currentZoomLevel = currentScope.minZoomLevel;

        if (isADS)
        {
            targetFOV = CalculateFOVForZoom(currentZoomLevel);
            targetCameraPosition = baseCameraPosition + currentScope.adsCameraPosition;
        }

        if (enableScopeModels)
            SpawnScopeModel();
    }

    void SpawnScopeModel()
    {
        if (spawnedScopeModel != null)
            Destroy(spawnedScopeModel);

        if (scopeMount == null || currentScope.scopeModelPrefab == null)
            return;

        spawnedScopeModel = Instantiate(currentScope.scopeModelPrefab, scopeMount);
        spawnedScopeModel.transform.localPosition = currentScope.modelPositionOffset;
        spawnedScopeModel.transform.localRotation = Quaternion.Euler(currentScope.modelRotationOffset);
    }

    void UpdateFOV()
    {
        if (currentScope == null) return;

        float speed = currentScope.fovTransitionSpeed;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * speed);
    }

    void UpdateCameraPosition()
    {
        if (currentScope == null) return;

        float speed = currentScope.adsTransitionSpeed;
        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition,
            targetCameraPosition,
            Time.deltaTime * speed);
    }

    public void OnWeaponChanged(WeaponData newWeapon)
    {
        if (newWeapon == null) return;

        currentWeapon = newWeapon;
        availableScopes = newWeapon.availableScopes;

        ExitADS();
        FindScopeMount();

        if (availableScopes != null && availableScopes.Count > 0)
        {
            currentScopeIndex = Mathf.Clamp(newWeapon.defaultScopeIndex, 0, availableScopes.Count - 1);
            EquipScope(currentScopeIndex);
        }
        else
        {
            currentScope = null;
            Debug.LogWarning("[Scope] Weapon '" + newWeapon.weaponName + "' has no scopes assigned!");
        }
    }

    // FIXED: Auto-create missing ScopeMount
    void FindScopeMount()
    {
        if (weaponManager == null) return;

        GameObject weaponObj = weaponManager.GetCurrentWeaponObject();
        if (weaponObj == null) return;

        Transform mount = weaponObj.transform.Find(currentWeapon.scopeMountName);

        if (mount != null)
        {
            scopeMount = mount;
            Debug.Log("[Scope] Found mount point: " + currentWeapon.scopeMountName);
        }
        else
        {
            // Auto-create missing scope mount
            GameObject mountObj = new GameObject(currentWeapon.scopeMountName);
            mountObj.transform.SetParent(weaponObj.transform);
            mountObj.transform.localPosition = new Vector3(0, 0.1f, 0.3f);
            mountObj.transform.localRotation = Quaternion.identity;
            scopeMount = mountObj.transform;

            Debug.Log("[Scope] Created missing mount point: " + currentWeapon.scopeMountName);
        }
    }

    void DrawReticle()
    {
        if (currentScope == null || currentScope.reticleTexture == null)
            return;

        if (currentScope.onlyShowWhenADS && !isADS)
            return;

        float halfSize = currentScope.reticleSize / 2f;
        Rect reticleRect = new Rect(
            Screen.width / 2f - halfSize,
            Screen.height / 2f - halfSize,
            currentScope.reticleSize,
            currentScope.reticleSize);

        GUI.color = currentScope.reticleColor;
        GUI.DrawTexture(reticleRect, currentScope.reticleTexture);
        GUI.color = Color.white;
    }

    void DrawDebugInfo()
    {
        GUILayout.BeginArea(new Rect(10, Screen.height - 180, 350, 170));
        GUILayout.Box("=== SCOPE DEBUG INFO ===");

        if (currentWeapon != null)
            GUILayout.Label("Weapon: " + currentWeapon.weaponName);

        if (currentScope != null)
        {
            GUILayout.Label("Scope: " + currentScope.scopeName + " (" + (currentScopeIndex + 1) + "/" + availableScopes.Count + ")");
            GUILayout.Label("Zoom: " + currentZoomLevel.ToString("F1") + "x (Range: " + currentScope.minZoomLevel.ToString("F1") + "x - " + currentScope.maxZoomLevel.ToString("F1") + "x)");
            GUILayout.Label("Scroll Zoom: " + (currentScope.allowScrollZoom ? "Enabled" : "Disabled"));
        }
        else
        {
            GUILayout.Label("Scope: None");
        }

        GUILayout.Label("FOV: " + playerCamera.fieldOfView.ToString("F1") + "deg -> " + targetFOV.ToString("F1") + "deg");
        GUILayout.Label("ADS: " + (isADS ? "YES" : "NO"));
        GUILayout.Label("Controls: RMB=ADS, B=Cycle, Scroll=Zoom");

        GUILayout.EndArea();
    }

    public bool IsADS() => isADS;
    public float GetCurrentZoom() => currentZoomLevel;
    public ScopeData GetCurrentScope() => currentScope;
    public void SetScopeIndex(int index) => EquipScope(index);
    public float GetADSMovementMultiplier() => currentScope != null ? currentScope.adsMovementMultiplier : 1f;
}
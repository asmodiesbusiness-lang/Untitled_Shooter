using UnityEngine;
using System.Collections.Generic;

public class ScopeManager : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private WeaponManager weaponManager;
    private Transform scopeMountPoint;

    [Header("=== INPUT ===")]
    [SerializeField] private KeyCode adsKey = KeyCode.Mouse1;

    private WeaponData currentWeapon;
    private List<ScopeData> availableScopes;
    private int currentScopeIndex = 0;
    private ScopeData currentScope;
    private GameObject spawnedScopeModel;

    private float currentZoomLevel = 1f;
    private float targetFOV;
    private float baseFOV = 60f;
    private bool isADS = false;

    private Vector3 baseCameraPosition;
    private Vector3 targetCameraPosition;

    [Header("=== DEBUG ===")]
    [SerializeField] private bool showDebugUI = true;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (weaponManager == null)
            weaponManager = FindFirstObjectByType<WeaponManager>();

        baseFOV = playerCamera.fieldOfView;
        targetFOV = baseFOV;
        baseCameraPosition = playerCamera.transform.localPosition;
        targetCameraPosition = baseCameraPosition;
    }

    void Update()
    {
        HandleInput();
        HandleScrollZoom();
        UpdateFOV();
        UpdateCameraPosition();
    }

    void HandleInput()
    {
        bool adsInput = Input.GetKey(adsKey);

        if (adsInput && !isADS)
            EnterADS();
        else if (!adsInput && isADS)
            ExitADS();
    }

    void HandleScrollZoom()
    {
        if (!isADS || currentScope == null || !currentScope.allowScrollZoom)
            return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoomLevel += scroll > 0 ? currentScope.zoomStepSize : -currentScope.zoomStepSize;
            currentZoomLevel = Mathf.Clamp(currentZoomLevel, currentScope.minZoomLevel, currentScope.maxZoomLevel);
            targetFOV = CalculateFOVForZoom(currentZoomLevel);
        }
    }

    void EnterADS()
    {
        isADS = true;

        if (currentScope != null)
        {
            currentZoomLevel = currentScope.minZoomLevel;
            targetFOV = CalculateFOVForZoom(currentZoomLevel);
            targetCameraPosition = baseCameraPosition + currentScope.adsCameraPosition;
        }
        else
        {
            // No scope - use default ADS
            targetFOV = baseFOV * 0.85f;
            targetCameraPosition = baseCameraPosition;
        }
    }

    void ExitADS()
    {
        isADS = false;
        currentZoomLevel = 1f;
        targetFOV = baseFOV;
        targetCameraPosition = baseCameraPosition;
    }

    float CalculateFOVForZoom(float zoomLevel)
    {
        if (currentScope == null) return baseFOV;

        float t = Mathf.InverseLerp(currentScope.minZoomLevel, currentScope.maxZoomLevel, zoomLevel);
        return Mathf.Lerp(currentScope.fovAtMinZoom, currentScope.fovAtMaxZoom, t);
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

        SpawnScopeModel();
    }

    void SpawnScopeModel()
    {
        if (spawnedScopeModel != null)
            Destroy(spawnedScopeModel);

        if (scopeMountPoint == null || currentScope == null || currentScope.scopeModelPrefab == null)
            return;

        spawnedScopeModel = Instantiate(currentScope.scopeModelPrefab, scopeMountPoint);
        spawnedScopeModel.transform.localPosition = currentScope.modelPositionOffset;
        spawnedScopeModel.transform.localRotation = Quaternion.Euler(currentScope.modelRotationOffset);
    }

    void UpdateFOV()
    {
        float currentFOV = playerCamera.fieldOfView;

        if (Mathf.Abs(currentFOV - targetFOV) < 0.5f)
        {
            playerCamera.fieldOfView = targetFOV;
        }
        else
        {
            float speed = currentScope != null ? currentScope.fovTransitionSpeed : 10f;
            playerCamera.fieldOfView = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * speed);
        }
    }

    void UpdateCameraPosition()
    {
        Vector3 currentPos = playerCamera.transform.localPosition;

        if (Vector3.Distance(currentPos, targetCameraPosition) < 0.005f)
        {
            playerCamera.transform.localPosition = targetCameraPosition;
        }
        else
        {
            float speed = currentScope != null ? currentScope.adsTransitionSpeed : 8f;
            playerCamera.transform.localPosition = Vector3.Lerp(currentPos, targetCameraPosition, Time.deltaTime * speed);
        }
    }

    public void OnWeaponChanged(WeaponData newWeapon)
    {
        if (newWeapon == null) return;

        currentWeapon = newWeapon;
        availableScopes = newWeapon.availableScopes;

        ExitADS();

        if (availableScopes != null && availableScopes.Count > 0)
        {
            currentScopeIndex = Mathf.Clamp(newWeapon.defaultScopeIndex, 0, availableScopes.Count - 1);
            EquipScope(currentScopeIndex);
        }
        else
        {
            currentScope = null;
        }
    }

    public void SetScopeMountPoint(Transform mountPoint)
    {
        scopeMountPoint = mountPoint;
        if (currentScope != null)
            SpawnScopeModel();
    }

    // === DEBUG UI ===
    void OnGUI()
    {
        if (!showDebugUI) return;

        // Get weapon info
        string weaponName = "None";
        int currentAmmo = 0;
        int reserveAmmo = 0;
        int magSize = 1;
        int magCount = 0;

        if (weaponManager != null)
        {
            WeaponController wc = weaponManager.GetCurrentWeapon();
            if (wc != null)
            {
                WeaponData wd = wc.GetWeaponData();
                if (wd != null)
                {
                    weaponName = wd.weaponName;
                    magSize = wd.magazineSize;
                }
                currentAmmo = wc.GetCurrentAmmo();
                reserveAmmo = wc.GetReserveAmmo();
                magCount = magSize > 0 ? reserveAmmo / magSize : 0;
            }
        }

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;

        // Main debug panel
        GUI.Box(new Rect(10, 10, 250, 220), "");
        GUILayout.BeginArea(new Rect(15, 15, 240, 210));

        GUILayout.Label("=== WEAPON SYSTEM DEBUG ===", headerStyle);
        GUILayout.Space(5);

        GUILayout.Label($"Weapon: {weaponName}", labelStyle);
        GUILayout.Label($"Ammo: {currentAmmo} / {magSize}", labelStyle);
        GUILayout.Label($"Reserve: {reserveAmmo} ({magCount} mags)", labelStyle);

        GUILayout.Space(10);
        GUILayout.Label("=== SCOPE ===", headerStyle);
        GUILayout.Label($"Scope: {(currentScope != null ? currentScope.scopeName : "None")}", labelStyle);
        GUILayout.Label($"ADS: {(isADS ? "YES" : "NO")}", labelStyle);
        GUILayout.Label($"FOV: {playerCamera.fieldOfView:F1}", labelStyle);
        GUILayout.Label($"Zoom: {currentZoomLevel:F1}x", labelStyle);
        GUILayout.Label($"Mount: {(scopeMountPoint != null ? "Assigned" : "NOT ASSIGNED")}", labelStyle);

        GUILayout.EndArea();
    }

    // === PUBLIC API ===
    public bool IsADS() => isADS;
    public float GetCurrentZoom() => currentZoomLevel;
    public ScopeData GetCurrentScope() => currentScope;
}
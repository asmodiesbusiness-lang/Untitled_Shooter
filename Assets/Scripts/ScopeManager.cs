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
    [SerializeField] private bool showDebugInfo = false;

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
    }

    void HandleInput()
    {
        bool adsInput = Input.GetButton("Fire2") || Input.GetKey(adsKey);

        if (adsInput && !isADS)
            EnterADS();
        else if (!adsInput && isADS)
            ExitADS();
    }

    void EnterADS()
    {
        if (currentScope == null) return;

        isADS = true;
        currentZoomLevel = currentScope.minZoomLevel;
        targetFOV = CalculateFOVForZoom(currentZoomLevel);
        targetCameraPosition = baseCameraPosition + currentScope.adsCameraPosition;
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
        if (currentScope == null) return;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * currentScope.fovTransitionSpeed);
    }

    void UpdateCameraPosition()
    {
        if (currentScope == null) return;

        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition,
            targetCameraPosition,
            Time.deltaTime * currentScope.adsTransitionSpeed);
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

    void DrawDebugInfo()
    {
        GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
        GUILayout.Box("=== SCOPE DEBUG INFO ===");

        if (currentScope != null)
            GUILayout.Label("Scope: " + currentScope.scopeName);
        else
            GUILayout.Label("Scope: None");

        GUILayout.Label("FOV: " + playerCamera.fieldOfView.ToString("F1"));
        GUILayout.Label("ADS: " + (isADS ? "YES" : "NO"));
        GUILayout.Label("Mount Point: " + (scopeMountPoint != null ? "Assigned" : "NOT ASSIGNED"));

        GUILayout.EndArea();
    }

    public bool IsADS() => isADS;
    public ScopeData GetCurrentScope() => currentScope;
}
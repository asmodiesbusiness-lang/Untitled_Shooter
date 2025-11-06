using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FIXED VERSION - All memory leaks and prefab corruption errors resolved
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Setup")]
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
    [SerializeField] private int startingWeaponIndex = 0;
    [SerializeField] private Transform weaponHolder;

    [Header("Shared Effects")]
    [SerializeField] private ParticleSystem sharedMuzzleFlash;
    [SerializeField] private GameObject impactEffect;

    [Header("Switching")]
    [SerializeField] private float switchTime = 0.5f;
    [SerializeField] private AnimationCurve switchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool blockSwitchingDuringADS = true;

    private FPSCharacterController characterController;
    private WeaponController currentWeaponController;
    private GameObject currentWeaponObject;
    private GameObject currentWeaponModel;
    private GameObject currentSuppressor;
    private int currentWeaponIndex = -1;
    private bool isSwitching = false;

    private Transform currentMuzzlePoint;
    private Transform currentCasingPoint;
    private Transform currentLaserOrigin;
    private Transform currentWeaponPivot;

    private ParticleSystem currentCasingSystem;
    private ParticleSystem currentMuzzleSmoke;

    private ScopeManager scopeManager;
    private WeaponData currentWeaponData;

    void Start()
    {
        characterController = GetComponentInParent<FPSCharacterController>();
        if (characterController == null)
        {
            Debug.LogError("WeaponManager: Could not find FPSCharacterController in parent!");
        }

        if (weaponHolder == null)
        {
            Debug.LogError("WeaponManager: weaponHolder not assigned!");
            return;
        }

        if (sharedMuzzleFlash == null)
        {
            Debug.LogWarning("WeaponManager: sharedMuzzleFlash not assigned!");
        }

        scopeManager = GetComponentInParent<ScopeManager>();
        if (scopeManager == null)
        {
            scopeManager = GetComponent<ScopeManager>();
        }
        if (scopeManager == null)
        {
            Debug.LogWarning("WeaponManager: No ScopeManager found! Scopes will not work.");
        }

        if (availableWeapons.Count > 0)
        {
            EquipWeapon(startingWeaponIndex);
        }
        else
        {
            Debug.LogError("WeaponManager: No weapons assigned!");
        }
    }

    void Update()
    {
        bool isADS = scopeManager != null && scopeManager.IsADS();
        bool canSwitchWeapons = !isSwitching && (!blockSwitchingDuringADS || !isADS);

        if (canSwitchWeapons)
        {
            for (int i = 0; i < Mathf.Min(availableWeapons.Count, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    if (i != currentWeaponIndex)
                    {
                        StartCoroutine(SwitchWeapon(i));
                    }
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Count;
                StartCoroutine(SwitchWeapon(nextIndex));
            }
            else if (scroll < 0f)
            {
                int prevIndex = (currentWeaponIndex - 1 + availableWeapons.Count) % availableWeapons.Count;
                StartCoroutine(SwitchWeapon(prevIndex));
            }
        }

        if (Input.GetKeyDown(KeyCode.L) && !isSwitching)
        {
            ToggleSuppressor();
        }
    }

    void EquipWeapon(int index)
    {
        if (index < 0 || index >= availableWeapons.Count) return;

        if (index == currentWeaponIndex && currentWeaponController != null)
        {
            return;
        }

        WeaponData weaponData = availableWeapons[index];

        if (weaponData == null)
        {
            Debug.LogError("WeaponData at index " + index + " is null!");
            return;
        }

        currentWeaponIndex = index;
        currentWeaponData = weaponData;

        // FIXED: Properly cleanup old particle systems
        if (currentCasingSystem != null)
        {
            currentCasingSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(currentCasingSystem.gameObject);
            currentCasingSystem = null;
        }

        if (currentMuzzleSmoke != null)
        {
            currentMuzzleSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(currentMuzzleSmoke.gameObject);
            currentMuzzleSmoke = null;
        }

        if (sharedMuzzleFlash != null)
        {
            sharedMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }

        currentWeaponObject = new GameObject(weaponData.weaponName);
        currentWeaponObject.transform.SetParent(weaponHolder);
        currentWeaponObject.transform.localPosition = weaponData.weaponHolderPosition;
        currentWeaponObject.transform.localRotation = Quaternion.Euler(weaponData.weaponHolderRotation);

        GameObject pivotObj = new GameObject("WeaponPivot");
        pivotObj.transform.SetParent(currentWeaponObject.transform);
        pivotObj.transform.localPosition = weaponData.pivotOffset;
        pivotObj.transform.localRotation = Quaternion.identity;
        currentWeaponPivot = pivotObj.transform;

        CharacterAnimatorController animController = characterController.GetComponent<CharacterAnimatorController>();
        if (animController != null)
        {
            animController.SetWeaponPivot(currentWeaponPivot);
            animController.SetWeaponData(weaponData);
        }

        WeaponCollisionSystem collisionSystem = characterController.GetComponent<WeaponCollisionSystem>();
        if (collisionSystem != null)
        {
            collisionSystem.SetWeaponPivot(currentWeaponPivot);
        }

        // FIXED: Create weapon model BEFORE attachment points
        if (weaponData.weaponModelPrefab != null)
        {
            currentWeaponModel = Instantiate(weaponData.weaponModelPrefab, currentWeaponPivot);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
        }

        // NOW create attachment points (after model exists)
        currentMuzzlePoint = weaponData.CreateAttachmentPoint(currentWeaponPivot, weaponData.muzzlePoint, "MuzzlePoint");
        currentCasingPoint = weaponData.CreateAttachmentPoint(currentWeaponPivot, weaponData.casingEjectionPoint, "CasingEjectionPoint");
        currentLaserOrigin = weaponData.CreateAttachmentPoint(currentWeaponPivot, weaponData.laserOriginPoint, "LaserOrigin");

        if (weaponData.casingPrefab != null)
        {
            currentCasingSystem = Instantiate(weaponData.casingPrefab, currentCasingPoint);
            currentCasingSystem.transform.localPosition = Vector3.zero;
            currentCasingSystem.transform.localRotation = Quaternion.identity;

            var main = currentCasingSystem.main;
            main.startSize = 0.05f * weaponData.casingSize;
            main.startSpeed = weaponData.casingEjectionForce;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
        }

        // FIXED: Don't reparent shared muzzle flash, just position it
        if (sharedMuzzleFlash != null)
        {
            sharedMuzzleFlash.transform.position = currentMuzzlePoint.position;
            sharedMuzzleFlash.transform.rotation = currentMuzzlePoint.rotation;
        }

        ParticleSystem smokeToUse = null;
        if (weaponData.standardSmokeEffect != null)
        {
            currentMuzzleSmoke = Instantiate(weaponData.standardSmokeEffect, currentMuzzlePoint);
            currentMuzzleSmoke.transform.localPosition = Vector3.zero;
            currentMuzzleSmoke.transform.localRotation = Quaternion.identity;
            smokeToUse = currentMuzzleSmoke;
        }

        currentWeaponController = currentWeaponObject.AddComponent<WeaponController>();

        if (characterController != null)
        {
            currentWeaponController.Initialize(characterController);
        }

        currentWeaponController.SetupFromData(
            weaponData,
            currentWeaponPivot,
            currentMuzzlePoint,
            currentCasingPoint,
            sharedMuzzleFlash,
            currentCasingSystem,
            impactEffect,
            smokeToUse,
            currentLaserOrigin
        );

        if (weaponData.hasSuppressor && weaponData.suppressorModel != null)
        {
            AttachSuppressor(weaponData);
        }

        if (scopeManager != null && currentWeaponData != null)
        {
            scopeManager.OnWeaponChanged(currentWeaponData);
        }
    }

    IEnumerator SwitchWeapon(int newIndex)
    {
        if (newIndex == currentWeaponIndex || isSwitching) yield break;

        isSwitching = true;

        Vector3 startPos = weaponHolder.localPosition;
        Vector3 loweredPos = startPos + Vector3.down * 0.5f;

        float elapsed = 0f;
        while (elapsed < switchTime / 2)
        {
            float t = switchCurve.Evaluate(elapsed / (switchTime / 2));
            weaponHolder.localPosition = Vector3.Lerp(startPos, loweredPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        EquipWeapon(newIndex);

        elapsed = 0f;
        while (elapsed < switchTime / 2)
        {
            float t = switchCurve.Evaluate(elapsed / (switchTime / 2));
            weaponHolder.localPosition = Vector3.Lerp(loweredPos, startPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponHolder.localPosition = startPos;
        isSwitching = false;

        if (scopeManager != null && currentWeaponData != null)
        {
            scopeManager.OnWeaponChanged(currentWeaponData);
        }
    }

    void AttachSuppressor(WeaponData weaponData)
    {
        if (currentSuppressor != null)
        {
            Destroy(currentSuppressor);
        }

        if (weaponData.suppressorModel != null && currentMuzzlePoint != null)
        {
            StartCoroutine(ScrewOnSuppressor(weaponData.suppressorModel));
        }
    }

    IEnumerator ScrewOnSuppressor(GameObject suppressorPrefab)
    {
        currentSuppressor = Instantiate(suppressorPrefab, currentMuzzlePoint);
        currentSuppressor.transform.localRotation = Quaternion.identity;

        Vector3 startPos = new Vector3(0, 0, -0.1f);
        Vector3 endPos = Vector3.zero;
        Quaternion startRot = Quaternion.Euler(0, 0, -720f);
        Quaternion endRot = Quaternion.identity;

        currentSuppressor.transform.localPosition = startPos;
        currentSuppressor.transform.localRotation = startRot;

        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            currentSuppressor.transform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
            currentSuppressor.transform.localRotation = Quaternion.Slerp(startRot, endRot, smoothT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        currentSuppressor.transform.localPosition = endPos;
        currentSuppressor.transform.localRotation = endRot;

        if (currentWeaponController != null)
        {
            currentWeaponController.SetSuppressed(true);
        }
    }

    void ToggleSuppressor()
    {
        WeaponData currentData = availableWeapons[currentWeaponIndex];

        if (currentSuppressor != null)
        {
            Destroy(currentSuppressor);
            if (currentWeaponController != null)
            {
                currentWeaponController.SetSuppressed(false);
            }
        }
        else if (currentData.hasSuppressor && currentData.suppressorModel != null)
        {
            AttachSuppressor(currentData);
        }
    }

    // FIXED: Add cleanup on destroy
    void OnDestroy()
    {
        if (currentCasingSystem != null)
        {
            currentCasingSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(currentCasingSystem.gameObject);
        }

        if (currentMuzzleSmoke != null)
        {
            currentMuzzleSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Destroy(currentMuzzleSmoke.gameObject);
        }

        if (sharedMuzzleFlash != null)
        {
            sharedMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public WeaponController GetCurrentWeapon()
    {
        return currentWeaponController;
    }

    public Transform GetCurrentWeaponPivot()
    {
        return currentWeaponPivot;
    }

    public void RefreshCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < availableWeapons.Count)
        {
            int temp = currentWeaponIndex;
            currentWeaponIndex = -1;
            EquipWeapon(temp);
        }
    }

    public WeaponData GetCurrentWeaponData()
    {
        return currentWeaponData;
    }

    public GameObject GetCurrentWeaponObject()
    {
        return currentWeaponObject;
    }
}
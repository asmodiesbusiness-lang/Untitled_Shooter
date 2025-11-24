using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple weapon manager - just handles switching between weapons
/// CharacterAnimatorController handles ADS/sway automatically via weaponHolder
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("=== WEAPON LIST ===")]
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();

    [Header("=== SETTINGS ===")]
    [SerializeField] private int currentWeaponIndex = 0;

    private GameObject currentWeaponInstance;
    private WeaponController currentWeaponController;

    void Start()
    {
        if (availableWeapons.Count > 0)
        {
            EquipWeapon(currentWeaponIndex);
        }
        else
        {
            Debug.LogError("[WeaponManager] No weapons in availableWeapons list!");
        }
    }

    void Update()
    {
        // Number key weapon switching
        if (Input.GetKeyDown(KeyCode.Alpha1) && availableWeapons.Count > 0)
        {
            SwitchToWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && availableWeapons.Count > 1)
        {
            SwitchToWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && availableWeapons.Count > 2)
        {
            SwitchToWeapon(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && availableWeapons.Count > 3)
        {
            SwitchToWeapon(3);
        }
    }

    void SwitchToWeapon(int index)
    {
        if (index == currentWeaponIndex) return;
        if (index < 0 || index >= availableWeapons.Count) return;

        // Unequip current weapon
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
            currentWeaponController = null;
        }

        // Equip new weapon
        currentWeaponIndex = index;
        EquipWeapon(currentWeaponIndex);

        Debug.Log($"[WeaponManager] Switched to: {availableWeapons[currentWeaponIndex].weaponName}");
    }

    void EquipWeapon(int index)
    {
        if (index < 0 || index >= availableWeapons.Count)
        {
            Debug.LogError($"[WeaponManager] Invalid weapon index: {index}");
            return;
        }

        WeaponData weaponData = availableWeapons[index];

        if (weaponData == null)
        {
            Debug.LogError($"[WeaponManager] WeaponData at index {index} is null!");
            return;
        }

        if (weaponData.weaponPrefab == null)
        {
            Debug.LogError($"[WeaponManager] {weaponData.weaponName} has no prefab assigned!");
            return;
        }

        // Instantiate weapon prefab
        currentWeaponInstance = Instantiate(weaponData.weaponPrefab, transform);
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;

        // Get WeaponController component
        currentWeaponController = currentWeaponInstance.GetComponent<WeaponController>();

        if (currentWeaponController == null)
        {
            Debug.LogError($"[WeaponManager] {weaponData.weaponName} prefab has no WeaponController component!");
            return;
        }

        // Initialize weapon with its data
        currentWeaponController.Initialize(weaponData);

        Debug.Log($"[WeaponManager] Equipped: {weaponData.weaponName}");
    }

    // Public API
    public WeaponController GetCurrentWeapon()
    {
        return currentWeaponController;
    }

    public WeaponData GetCurrentWeaponData()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < availableWeapons.Count)
        {
            return availableWeapons[currentWeaponIndex];
        }
        return null;
    }

    public GameObject GetCurrentWeaponGameObject()
    {
        return currentWeaponInstance;
    }
}
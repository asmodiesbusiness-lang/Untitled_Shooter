using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("=== WEAPON SYSTEM ===")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponData[] availableWeapons;
    [SerializeField] private int startingWeaponIndex = 0;

    [Header("=== SWITCHING ===")]
    [SerializeField] private float switchDelay = 0.5f;
    [SerializeField] private KeyCode nextWeaponKey = KeyCode.E;
    [SerializeField] private KeyCode previousWeaponKey = KeyCode.Q;

    private WeaponController currentWeapon;
    private WeaponData currentWeaponData;
    private int currentWeaponIndex = 0;
    private bool isSwitching = false;
    private float switchTimer = 0f;

    private ScopeManager scopeManager;

    void Start()
    {
        scopeManager = FindFirstObjectByType<ScopeManager>();

        if (availableWeapons.Length > 0)
        {
            currentWeaponIndex = Mathf.Clamp(startingWeaponIndex, 0, availableWeapons.Length - 1);
            EquipWeapon(currentWeaponIndex);
        }
    }

    void Update()
    {
        if (isSwitching)
        {
            switchTimer += Time.deltaTime;
            if (switchTimer >= switchDelay)
            {
                isSwitching = false;
                switchTimer = 0f;
            }
            return;
        }

        if (Input.GetKeyDown(nextWeaponKey))
            SwitchWeapon(1);

        if (Input.GetKeyDown(previousWeaponKey))
            SwitchWeapon(-1);
    }

    void SwitchWeapon(int direction)
    {
        if (isSwitching || availableWeapons.Length <= 1) return;

        currentWeaponIndex = (currentWeaponIndex + direction + availableWeapons.Length) % availableWeapons.Length;
        EquipWeapon(currentWeaponIndex);
    }

    void EquipWeapon(int index)
    {
        if (index < 0 || index >= availableWeapons.Length) return;

        if (currentWeapon != null)
            Destroy(currentWeapon.gameObject);

        WeaponData weaponData = availableWeapons[index];
        if (weaponData.weaponPrefab == null)
        {
            Debug.LogError("[WeaponManager] Weapon prefab is null!");
            return;
        }

        GameObject weaponObj = Instantiate(weaponData.weaponPrefab, weaponHolder);
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;
        weaponObj.transform.localScale = Vector3.one;

        currentWeapon = weaponObj.GetComponent<WeaponController>();
        currentWeaponData = weaponData;

        if (currentWeapon != null)
            currentWeapon.Initialize(weaponData);

        if (scopeManager != null)
        {
            Transform scopeMount = weaponObj.transform.Find("ScopeMount");
            if (scopeMount != null)
                scopeManager.SetScopeMountPoint(scopeMount);

            scopeManager.OnWeaponChanged(weaponData);
        }

        isSwitching = true;
        switchTimer = 0f;
    }

    public WeaponController GetCurrentWeapon() => currentWeapon;
    public WeaponData GetCurrentWeaponData() => currentWeaponData;
    public bool IsSwitching() => isSwitching;
}
using UnityEngine;

public class DualPointRecoil : MonoBehaviour
{
    [Header("=== AUTO SETUP ===")]
    [SerializeField] private bool autoSetup = true;

    [Header("=== PIVOT POINTS ===")]
    public Transform weaponRoot;
    public Transform weaponModel;

    [Header("=== GRIP POSITION ===")]
    [SerializeField] private Vector3 gripLocalPosition = new Vector3(0, -0.1f, -0.2f);

    [Header("=== STOCK ROTATION ===")]
    [SerializeField] private float stockRotationAmount = 2f;
    [SerializeField] private float stockRotationSpeed = 8f;

    [Header("=== BARREL KICK ===")]
    [SerializeField] private float barrelKickAmount = 0.05f;
    [SerializeField] private float barrelKickSpeed = 10f;

    [Header("=== RECOVERY ===")]
    [SerializeField] private float recoverySpeed = 5f;

    private Vector3 stockOriginalRotation;
    private Vector3 stockTargetRotation;
    private Vector3 stockCurrentRotation;

    private Vector3 modelOriginalPosition;
    private Vector3 modelTargetPosition;
    private Vector3 modelCurrentPosition;

    void Start()
    {
        if (autoSetup)
            AutoSetupWeaponHierarchy();

        if (weaponRoot != null)
        {
            stockOriginalRotation = weaponRoot.localEulerAngles;
            stockCurrentRotation = stockOriginalRotation;
            stockTargetRotation = stockOriginalRotation;
        }

        if (weaponModel != null)
        {
            modelOriginalPosition = weaponModel.localPosition;
            modelCurrentPosition = modelOriginalPosition;
            modelTargetPosition = modelOriginalPosition;
        }
    }

    void AutoSetupWeaponHierarchy()
    {
        Transform existingRoot = transform.Find("WeaponRoot");
        if (existingRoot != null)
        {
            weaponRoot = existingRoot;
            if (existingRoot.childCount > 0)
                weaponModel = existingRoot.GetChild(0);
            return;
        }

        Transform visualModel = null;
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length > 0)
            visualModel = renderers[0].transform;

        if (visualModel == null)
            return;

        GameObject rootObj = new GameObject("WeaponRoot");
        rootObj.transform.SetParent(transform);
        rootObj.transform.localPosition = gripLocalPosition;
        rootObj.transform.localRotation = Quaternion.identity;
        rootObj.transform.localScale = Vector3.one;

        Vector3 worldPos = visualModel.position;
        Quaternion worldRot = visualModel.rotation;

        visualModel.SetParent(rootObj.transform);
        visualModel.position = worldPos;
        visualModel.rotation = worldRot;

        weaponRoot = rootObj.transform;
        weaponModel = visualModel;
    }

    void Update()
    {
        if (weaponRoot == null || weaponModel == null) return;

        stockCurrentRotation = Vector3.Lerp(stockCurrentRotation, stockTargetRotation, Time.deltaTime * stockRotationSpeed);
        stockTargetRotation = Vector3.Lerp(stockTargetRotation, stockOriginalRotation, Time.deltaTime * recoverySpeed);
        weaponRoot.localEulerAngles = stockCurrentRotation;

        modelCurrentPosition = Vector3.Lerp(modelCurrentPosition, modelTargetPosition, Time.deltaTime * barrelKickSpeed);
        modelTargetPosition = Vector3.Lerp(modelTargetPosition, modelOriginalPosition, Time.deltaTime * recoverySpeed);
        weaponModel.localPosition = modelCurrentPosition;
    }

    public void ApplyRecoil(float multiplier = 1f)
    {
        if (weaponRoot == null || weaponModel == null) return;

        stockTargetRotation.x -= stockRotationAmount * multiplier;
        modelTargetPosition.z -= barrelKickAmount * multiplier;
        modelTargetPosition.y -= barrelKickAmount * 0.5f * multiplier;
    }

    public void ApplyRecoil(Vector2 recoilPattern)
    {
        if (weaponRoot == null || weaponModel == null) return;

        stockTargetRotation.x -= recoilPattern.x;
        stockTargetRotation.y += recoilPattern.y;
        modelTargetPosition.z -= barrelKickAmount;
        modelTargetPosition.y -= barrelKickAmount * 0.5f;
    }

    public void ResetRecoil()
    {
        stockCurrentRotation = stockOriginalRotation;
        stockTargetRotation = stockOriginalRotation;
        modelCurrentPosition = modelOriginalPosition;
        modelTargetPosition = modelOriginalPosition;
    }
}
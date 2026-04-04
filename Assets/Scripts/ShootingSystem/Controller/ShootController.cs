using UnityEngine;
using UnityEngine.InputSystem;

public class ShootController : MonoBehaviour
{
    public InputActionReference ShootAction;
    private WeaponRuntimeInstance weaponRuntimeInstance;
    private GameObject currentWeaponModel;
    private Transform muzzleTransform;
    private Transform trailMarker;
    private Transform weaponOwner;
    [SerializeField] private WeaponFireConfig initialWeaponConfig;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private Transform fireOrigin;


    void Start()
    {
        EquipWeapon(initialWeaponConfig);
        weaponOwner = transform;
    }

    void Update()
    {
        if (weaponRuntimeInstance == null)
        {
            return;
        }

        bool isPressed = ShootAction.action.IsPressed();
        bool wasPressed = ShootAction.action.WasPressedThisFrame();

        weaponRuntimeInstance.TryFire(isPressed, wasPressed, fireOrigin, weaponOwner , this);
    }

    private void EquipWeapon(WeaponFireConfig weaponConfig)
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }

        if (weaponConfig.weaponVisualConfig != null && weaponConfig.weaponVisualConfig.weaponModel != null)
        {
            currentWeaponModel = Instantiate(weaponConfig.weaponVisualConfig.weaponModel, weaponSocket);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
            muzzleTransform = currentWeaponModel.GetComponentInChildren<MuzzleMarker>()?.transform;
            trailMarker = currentWeaponModel.GetComponentInChildren<TrailMarker>()?.transform;
            if (muzzleTransform == null)
            {
                Debug.LogError("[ShootController] MuzzleMarker missing on weapon prefab.");
            }
            weaponRuntimeInstance = new WeaponRuntimeInstance(weaponConfig, muzzleTransform , trailMarker);
        }

    }
}

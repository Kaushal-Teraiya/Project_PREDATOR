using UnityEngine;
using UnityEngine.InputSystem;

public class ShootController : MonoBehaviour
{
    public InputActionReference ShootAction;
    public InputActionReference ReloadAction;
    private WeaponRuntimeInstance weaponRuntimeInstance;
    private AmmoUI ammoUI;
    private GameObject currentWeaponModel;
    private Transform muzzleTransform;
    private Transform trailMarker;
    private Transform weaponOwner;
    private MonoBehaviour coroutineRunner;
    private PlayerLook playerLook;
    private Vector3 defaultWeaponPosition;
    private Vector3 defaultArmsPosition;
    private float armsKickback;
    private Animator weaponAnimator;
    [SerializeField] private WeaponFireConfig initialWeaponConfig;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private Transform weaponRoot;


    void Awake()
    {
        ammoUI = GetComponentInChildren<AmmoUI>();
        playerLook = GetComponent<PlayerLook>();
        weaponAnimator = GetComponentInChildren<Animator>();
        coroutineRunner = this;
    }
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
        if (ReloadAction.action.WasPressedThisFrame())
        {
            weaponRuntimeInstance.RequestReload();
        }
        if (currentWeaponModel != null)
        {
            var recoilConfig = weaponRuntimeInstance
                .WeaponFireConfig
                .weaponVisualConfig
                .recoilConfig;

            currentKickback = Mathf.MoveTowards(
                currentKickback,
                0f,
                recoilConfig.weaponKickbackRecoverySpeed * Time.deltaTime
            );

            currentWeaponModel.transform.localPosition =
                defaultWeaponPosition - Vector3.up * currentKickback;
        }

        if (weaponRoot != null)
        {
            var recoilConfig = weaponRuntimeInstance
                .WeaponFireConfig
                .weaponVisualConfig
                .recoilConfig;

            armsKickback = Mathf.MoveTowards(
                armsKickback,
                0f,
                recoilConfig.weaponKickbackRecoverySpeed * Time.deltaTime
            );

            weaponRoot.localPosition =
                defaultArmsPosition + Vector3.back * armsKickback;
        }
        weaponRuntimeInstance.TryFire(isPressed, wasPressed, fireOrigin, weaponOwner, ApplyRecoilKickBack, weaponAnimator);
    }

    private void EquipWeapon(WeaponFireConfig weaponConfig)
    {
        if (currentWeaponModel != null)
        {
            Destroy(currentWeaponModel);
        }

        if (weaponConfig.weaponVisualConfig != null && weaponConfig.weaponVisualConfig.weaponModel != null)
        {
            currentWeaponModel = Instantiate(weaponConfig.weaponVisualConfig.weaponModel, weaponSocket, false);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
            muzzleTransform = currentWeaponModel.GetComponentInChildren<MuzzleMarker>()?.transform;
            trailMarker = currentWeaponModel.GetComponentInChildren<TrailMarker>()?.transform;

            if (muzzleTransform == null)
            {
                Debug.LogError("[ShootController] MuzzleMarker missing on weapon prefab.");
            }

            weaponRuntimeInstance = new WeaponRuntimeInstance(weaponConfig, muzzleTransform, trailMarker, coroutineRunner, playerLook, weaponAnimator);

            if (ammoUI != null)
            {
                ammoUI.SetWeapon(weaponRuntimeInstance);
            }

            defaultWeaponPosition = currentWeaponModel.transform.localPosition;
            defaultArmsPosition = weaponRoot.transform.localPosition;
        }
    }


    private float currentKickback;

    private void ApplyRecoilKickBack(RecoilConfig recoilConfig)
    {
        if (currentWeaponModel == null)
            return;

        if (weaponRoot == null)
        {
            return;
        }
        armsKickback = recoilConfig.weaponKickbackDistance;
        currentKickback = recoilConfig.weaponKickbackDistance;
    }
}

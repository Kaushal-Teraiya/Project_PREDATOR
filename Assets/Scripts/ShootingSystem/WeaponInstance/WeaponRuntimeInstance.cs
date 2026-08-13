using System.Collections;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class WeaponRuntimeInstance
{
    private WeaponFireConfig weaponFireConfig;
    public WeaponFireConfig WeaponFireConfig => weaponFireConfig;
    private float nextAllowedFireTime;
    private float cooldownDuration;
    private IFireModeExecutor fireModeExecutor;
    private Transform muzzleTransform;
    private Transform trailMarker;
    private MonoBehaviour coroutineRunner;
    private bool isReloading;
    public int currentAmmo { get; private set; }
    public int remainingMagazines { get; private set; }
    private PlayerLook playerLook;
    private Animator weaponAnimator;
    private AudioSource weaponAudioSource;

    public WeaponRuntimeInstance(WeaponFireConfig weaponFireConfig, Transform muzzleTransform, Transform trailMarker, MonoBehaviour coroutineRunner, PlayerLook playerLook, Animator weaponAnimator, AudioSource weaponAudioSource)
    {
        this.weaponFireConfig = weaponFireConfig;
        this.muzzleTransform = muzzleTransform;
        this.trailMarker = trailMarker;
        this.coroutineRunner = coroutineRunner;
        this.playerLook = playerLook;
        this.weaponAnimator = weaponAnimator;
        this.weaponAudioSource = weaponAudioSource;
        currentAmmo = weaponFireConfig.maxAmmoCapacity;
        remainingMagazines = weaponFireConfig.maxMagazineCapacity;

        cooldownDuration = 1 / weaponFireConfig.fireRate;
        fireModeExecutor = FireModeExecutorFactory.Create(GetFireMode());

        if (fireModeExecutor == null)
        {
            Debug.LogError("[WeaponRuntimeInstance] fireModeExecutor is null");
        }
    }

    public void TryFire(bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner, System.Action<RecoilConfig> onShotFired, Animator weaponAnimator)
    {
        fireModeExecutor.TryExecuteFire(this, isPressed, wasPressed, fireOrigin, owner, coroutineRunner, onShotFired, weaponAnimator);
    }

    public bool PerformShot(Transform fireOrigin, Transform owner)
    {
        if (Time.time < nextAllowedFireTime)
        {
            return false;
        }

        if (!canShoot())
        {
            return false;
        }

        var recoilConfig = weaponFireConfig.weaponVisualConfig.recoilConfig;
        owner.GetComponent<SoundEmitter>()?.EmitSound(weaponFireConfig.soundProfile);
        weaponAudioSource.PlayOneShot(weaponFireConfig.weaponVisualConfig.weaponSoundConfig.shootSound);
        SpawnMuzzleFlash();
        HandleAmmo();
        //   playerLook.ApplyRecoil(recoilConfig.verticleRecoil, recoilConfig.horizontalRecoil);
        //  playerLook.SetRecoverySpeed(recoilConfig.recoilRecoverySpeed);

        nextAllowedFireTime = Time.time + cooldownDuration;

        HitResult hitResult;
        Vector3 spreadDirection = GetSpreadDirection(fireOrigin);
        if (weaponFireConfig.hitDetector.DetectHit(fireOrigin, spreadDirection, weaponFireConfig, out hitResult))
        {
            Transform t = hitResult.hitObject.transform;
            LimbHitBox limbHitBox = hitResult.hitObject.GetComponent<LimbHitBox>();
            if (limbHitBox != null)
            {
                if (GameplaySettings.dismembermentEnabled && limbHitBox.GetComponent<Dismembered>() == null)
                {
                    string boneName = limbHitBox.bone.name.ToLower();
                    if (!boneName.Contains("mixamorig:hips") && !boneName.Contains("mixamorig:leftshoulder") && !boneName.Contains("mixamorig:rightshoulder"))
                    {
                        limbHitBox.dismemberment.DismemberBone(limbHitBox.bone.name);
                    }

                }
            }

            IDamageable damageable = hitResult.hitObject.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                var ownerDamagable = owner.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(hitResult, weaponFireConfig.damage);
                }
            }


        }
        SpawnTrail(hitResult, spreadDirection, fireOrigin, trailMarker);
        return true;
    }

    public FireMode GetFireMode()
    {
        return weaponFireConfig.fireMode;
    }

    private void SpawnMuzzleFlash()
    {
        if (weaponFireConfig.weaponVisualConfig == null) return;

        if (weaponFireConfig.weaponVisualConfig.muzzleFlash == null) return;

        if (muzzleTransform == null) return;

        var muzzleFlash = UnityEngine.GameObject.Instantiate(
            weaponFireConfig.weaponVisualConfig.muzzleFlash,
            muzzleTransform.position,
            muzzleTransform.rotation,
            muzzleTransform);

        muzzleFlash.transform.localPosition = Vector3.zero;
        muzzleFlash.transform.localRotation = Quaternion.identity;
        muzzleFlash.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);


        GameObject.Destroy(muzzleFlash, 2f);
    }

    private void SpawnTrail(HitResult hitResult, Vector3 spreadDirection, Transform fireOrigin, Transform trailMarker)
    {
        var visualConfig = weaponFireConfig.weaponVisualConfig;

        Vector3 endPoint;
        if (visualConfig == null || visualConfig.trailConfig == null || trailMarker == null)
        {
            return;
        }
        if (hitResult.success)
        {
            endPoint = hitResult.hitPosition;
        }
        else
        {
            endPoint = trailMarker.position + spreadDirection * visualConfig.trailConfig.missDistance;
        }
        TrailController.SpawnTrail(trailMarker, endPoint, visualConfig.trailConfig, coroutineRunner);
    }

    public Vector3 GetSpreadDirection(Transform fireOrigin)
    {
        if (!weaponFireConfig.usesSpread)
        {
            return fireOrigin.forward;
        }

        Vector3 spreadDirection = fireOrigin.forward;

        spreadDirection += new Vector3(
            Random.Range(-weaponFireConfig.bulletSpread.x, weaponFireConfig.bulletSpread.x),
            Random.Range(-weaponFireConfig.bulletSpread.y, weaponFireConfig.bulletSpread.y),
            Random.Range(-weaponFireConfig.bulletSpread.z, weaponFireConfig.bulletSpread.z)
        );

        return spreadDirection;
    }

    public void HandleAmmo()
    {
        currentAmmo--;

        if (remainingMagazines < 0)
        {
            currentAmmo = 0;
            remainingMagazines = 0;
            Debug.Log("[WeaponRuntimeInstance] Ammo Over");
            return;
        }
        if (currentAmmo <= 0)
        {
            RequestReload();
        }

    }

    private IEnumerator Reload()
    {
        if (remainingMagazines <= 0)
        {
            Debug.Log("[WeaponRuntimeInstance] No Ammo Left");
            yield break;
        }
        isReloading = true;
        weaponAnimator.SetBool("isReloading", isReloading);
        weaponAnimator.SetTrigger("Reload");
        yield return null;
        if (!weaponAudioSource.isPlaying)
        {
            weaponAudioSource.PlayOneShot(WeaponFireConfig.weaponVisualConfig.weaponSoundConfig.reloadSound);
        }

        yield return new WaitUntil(() => weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Reload"));
        yield return new WaitUntil(() => weaponAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);
        // float reloadDuration = (weaponAnimator.GetCurrentAnimatorStateInfo(0).length);
        // yield return new WaitForSeconds(reloadDuration);
        remainingMagazines--;
        currentAmmo = weaponFireConfig.maxAmmoCapacity;
        isReloading = false;
        weaponAnimator.SetBool("isReloading", false);
    }

    private bool canShoot()
    {
        if (currentAmmo > 0 && currentAmmo <= weaponFireConfig.maxAmmoCapacity && !isReloading)
        {
            return true;
        }

        return false;
    }

    public void RequestReload()
    {
        if (isReloading)
        {
            Debug.Log("[WeaponRuntimeInstance] Cannot Reload at this moment.");
            return;
        }

        if (currentAmmo == weaponFireConfig.maxAmmoCapacity)
        {
            Debug.Log("[WeaponRuntimeInstance] Cannot Reload at this moment.");
            return;
        }

        if (remainingMagazines <= 0)
        {
            Debug.Log("[WeaponRuntimeInstance] Cannot Reload at this moment.");
            return;
        }

        coroutineRunner.StartCoroutine(Reload());
    }

}


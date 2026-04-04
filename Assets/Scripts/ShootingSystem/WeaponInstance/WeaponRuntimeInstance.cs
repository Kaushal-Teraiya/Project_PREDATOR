using System.Collections;
using System.Net;
using UnityEngine;

public class WeaponRuntimeInstance
{
    private WeaponFireConfig weaponFireConfig;
    public WeaponFireConfig WeaponFireConfig => weaponFireConfig;
    private float nextAllowedFireTime;
    private float cooldownDuration;
    private IFireModeExecutor fireModeExecutor;
    private Transform muzzleTransform;
    private Transform trailMarker;

    public WeaponRuntimeInstance(WeaponFireConfig weaponFireConfig, Transform muzzleTransform, Transform trailMarker)
    {
        this.weaponFireConfig = weaponFireConfig;
        this.muzzleTransform = muzzleTransform;
        this.trailMarker = trailMarker;
        cooldownDuration = 1 / weaponFireConfig.fireRate;
        fireModeExecutor = FireModeExecutorFactory.Create(GetFireMode());

        if (fireModeExecutor == null)
        {
            Debug.LogError("[WeaponRuntimeInstance] fireModeExecutor is null");
        }
    }

    public void TryFire(bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner, MonoBehaviour coroutineRuinner)
    {
        fireModeExecutor.TryExecuteFire(this, isPressed, wasPressed, fireOrigin, owner, coroutineRuinner);
    }

    public bool PerformShot(Transform fireOrigin, Transform owner, MonoBehaviour coroutineRunner)
    {
        if (Time.time < nextAllowedFireTime)
        {
            return false;
        }

        owner.GetComponent<SoundEmitter>()?.EmitSound(weaponFireConfig.soundProfile);
        SpawnMuzzleFlash();
        nextAllowedFireTime = Time.time + cooldownDuration;

        HitResult hitResult;
        Vector3 spreadDirection = GetSpreadDirection(fireOrigin);
        if (weaponFireConfig.hitDetector.DetectHit(fireOrigin, spreadDirection, weaponFireConfig, out hitResult))
        {
            IDamageable damageable = hitResult.hitObject.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                var ownerDamageable = owner.GetComponentInParent<IDamageable>();

                if (damageable != ownerDamageable)
                {
                    Debug.Log("[WeaponRuntimeInstance] Applying Damage to Enemies.");
                    damageable.TakeDamage(weaponFireConfig.damage);
                }
            }
            else
            {
                Debug.Log("[WeaponRuntimeInstance] damageable is NUll");
            }
        }
        SpawnTrail(hitResult, spreadDirection, fireOrigin, coroutineRunner, trailMarker);
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

    private void SpawnTrail(HitResult hitResult, Vector3 spreadDirection, Transform fireOrigin, MonoBehaviour coroutineRunner, Transform trailMarker)
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
}


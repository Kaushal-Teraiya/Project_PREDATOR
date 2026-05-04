using UnityEngine;

public class FireMode_BurstExecutor : IFireModeExecutor
{
    private bool isBurstActive;
    private int remainingShots;
    private float nextAllowedBurstTime;

    public void TryExecuteFire(WeaponRuntimeInstance weapon, bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner, MonoBehaviour coroutineRunner, System.Action<RecoilConfig> onShotFired, Animator weaponAnimator)
    {
        if (!isBurstActive)
        {
            if (isPressed && IsReadyForNextBurst(weapon))
            {
                StartBurst(weapon);
            }
        }

        if (isBurstActive)
        {
            bool fired = weapon.PerformShot(fireOrigin, owner);
            if (fired)
            {
                var recoilConfig = weapon.WeaponFireConfig.weaponVisualConfig.recoilConfig;
                onShotFired?.Invoke(recoilConfig);
                remainingShots--;
            }

            if (remainingShots == 0)
            {
                isBurstActive = false;
                nextAllowedBurstTime = Time.time + weapon.WeaponFireConfig.burstCycleCooldown;
            }
        }

    }

    private void StartBurst(WeaponRuntimeInstance weapon)
    {
        isBurstActive = true;
        remainingShots = weapon.WeaponFireConfig.burstCount;
    }

    private bool IsReadyForNextBurst(WeaponRuntimeInstance weapon)
    {
        return Time.time >= nextAllowedBurstTime;
    }
}

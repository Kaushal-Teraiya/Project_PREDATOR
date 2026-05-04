using UnityEngine;

public class FireMode_AutomaticExecutor : IFireModeExecutor
{
    public void TryExecuteFire(WeaponRuntimeInstance weapon, bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner, MonoBehaviour coroutineRunner, System.Action<RecoilConfig> onShotFired, Animator weaponAnimator)
    {
        weaponAnimator.SetBool("isFiring", isPressed);
        
        if (!isPressed)
        {
            return;
        }

        bool fired = weapon.PerformShot(fireOrigin, owner);
        if (fired)
        {
            var recoilConfig = weapon.WeaponFireConfig.weaponVisualConfig.recoilConfig;
            onShotFired?.Invoke(recoilConfig);
        }
    }
}

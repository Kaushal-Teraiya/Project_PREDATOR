using UnityEngine;

public class FireMode_SemiAutoExecutor : IFireModeExecutor
{
    public void TryExecuteFire(WeaponRuntimeInstance weapon, bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner, MonoBehaviour coroutineRunner, System.Action<RecoilConfig> onShotFired, Animator weaponAnimator)
    {
        if (wasPressed)
        {
            bool fired = weapon.PerformShot(fireOrigin, owner);
            if (fired)
            {
                var recoilConfig = weapon.WeaponFireConfig.weaponVisualConfig.recoilConfig;
                onShotFired?.Invoke(recoilConfig);
            }
        }
    }
}

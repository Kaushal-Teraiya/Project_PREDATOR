using UnityEngine;

public class FireMode_SemiAutoExecutor : IFireModeExecutor
{
    public void TryExecuteFire(WeaponRuntimeInstance weapon, bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner, MonoBehaviour coroutineRunner)
    {
        if (wasPressed)
        {
            weapon.PerformShot(fireOrigin, owner, coroutineRunner);
        }
    }
}

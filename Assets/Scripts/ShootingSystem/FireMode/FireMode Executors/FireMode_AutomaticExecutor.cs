using UnityEngine;

public class FireMode_AutomaticExecutor : IFireModeExecutor
{
    public void TryExecuteFire(WeaponRuntimeInstance weapon, bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner , MonoBehaviour coroutineRunner)
    {
        if (isPressed)
        {
            weapon.PerformShot(fireOrigin, owner , coroutineRunner);
        }

    }
}

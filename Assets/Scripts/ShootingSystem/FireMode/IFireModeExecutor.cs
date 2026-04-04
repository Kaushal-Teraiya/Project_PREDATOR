using UnityEngine;
public interface IFireModeExecutor
{
   public void TryExecuteFire(WeaponRuntimeInstance weapon, bool isPressed, bool wasPressed, Transform fireOrigin, Transform owner , MonoBehaviour coroutineRunner);
}

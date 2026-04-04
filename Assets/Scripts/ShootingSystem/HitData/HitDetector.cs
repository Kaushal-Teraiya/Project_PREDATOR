using UnityEngine;

public abstract class HitDetector : ScriptableObject
{
   public abstract bool DetectHit(Transform firePoint, Vector3 direction, WeaponFireConfig config, out HitResult hitResult);
}

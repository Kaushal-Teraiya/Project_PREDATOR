using UnityEngine;

public interface IDamageable
{
    void TakeDamage(HitResult hitResult, float amount);
}

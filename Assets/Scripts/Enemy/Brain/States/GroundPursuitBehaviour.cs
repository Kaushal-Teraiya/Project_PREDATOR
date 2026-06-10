using UnityEngine;

public class GroundPursuitBehaviour : IPursuitBehaviour
{
    public Vector3 GetPursuitTarget(EnemyBrain brain)
    {
        return PursuitUtility.GetAttackTargetOffset(brain);
    }
}

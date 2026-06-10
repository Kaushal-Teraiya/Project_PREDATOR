using UnityEngine;

public class CrawlerPursuitBehaviour : IPursuitBehaviour
{
    public Vector3 GetPursuitTarget(EnemyBrain brain)
    {
        return PursuitUtility.GetAttackTargetOffset(brain);
    }
}

using UnityEngine;
public static class PursuitUtility
{
    public static Vector3 GetAttackTargetOffset(EnemyBrain brain)
    {
        var dir = brain.transform.position - brain.player.transform.position;
        dir.Normalize();
        var offset = brain.AttackDistance - brain.AttackOffset;
        var desiredPoint = brain.player.transform.position + dir * offset;
        return desiredPoint;
    }
}

using UnityEngine;

public class AttackState : IEnemyState
{
    private EnemyBrain brain;
    public AttackState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        brain.enemyMovement.Stop();
        brain.enemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, brain.player.transform.position);
        brain.Animator_SetTrigger("Attack");
        Debug.Log("[Attack] In Attack..");
    }
    public void Tick()
    {

    }
    public void OnExit()
    {
        brain.enemyMovement.SetRotationPermission(true);
    }

    public void HandleAttackHit()
    {
        if (TargetInRange(brain.player))
        {
            IDamageable damagable = brain.player.GetComponent<IDamageable>();
            if (damagable != null)
            {
                damagable.TakeDamage(brain.attackDamage);
            }
            Debug.Log("[Attack] Damage Applied.");
        }
    }

    public void HandleAttackEnd()
    {
        brain.SwitchState(brain.ChaseState);
    }

    private bool TargetInRange(GameObject target)
    {
        var distance = Vector3.Distance(brain.transform.position, target.transform.position);
        return distance <= brain.attackDistance;
    }
}

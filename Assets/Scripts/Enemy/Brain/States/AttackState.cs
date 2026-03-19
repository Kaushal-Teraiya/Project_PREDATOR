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
        var Enemy = brain.enemyMovement;
        Enemy.Stop();
        Enemy.RotationIntent(EnemyMovement.RotationPriority.State, brain.player.transform.position);
        Enemy.SetMovementMode(EnemyMovement.MovementMode.Idle);
        Debug.Log("[Attack] In Attack..");
    }
    public void Tick()
    {
        var Enemy = brain.enemyMovement;
        Enemy.RequestAnimation(new AnimationIntent(AnimationType.Attack, 30));
       
    }
    public void OnExit()
    {
        brain.enemyMovement.SetRotationPermission(true);
        brain.enemyMovement.Animator_ResetTrigger("Attack");
    }

    public void HandleAttackHit()
    {
        if (TargetInRange(brain.player))
        {
            IDamageable damagable = brain.player.GetComponent<IDamageable>();
            if (damagable != null)
            {
                damagable.TakeDamage(brain.AttackDamage);
            }
            Debug.Log("[Attack] Damage Applied.");
        }
    }

    public void HandleAttackEnd()
    {
        brain.NotifyAttackEnded();
        float distance = Vector3.Distance(brain.transform.position, brain.player.transform.position);

        if (distance > brain.AttackDistance)
        {
            brain.SwitchState(brain.ChaseState);
            return;
        }

        StartNextAttack();
    }

    private void StartNextAttack()
    {
        brain.enemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Attack, 30));
    }

    private bool TargetInRange(GameObject target)
    {
        var distance = Vector3.Distance(brain.transform.position, target.transform.position);
        return distance <= brain.AttackDistance;
    }
}

using UnityEngine;

public class AttackState : IEnemyState
{
    private EnemyBrain brain;
    private float attackTime = 3f;
    private bool damageApplied;
    private float attackTimer;
    public AttackState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        attackTimer = 0f;
        damageApplied = false;
        brain.enemyMovement.Stop();
        Debug.Log("[Attack] In Attack..");
    }
    public void Tick()
    {
        attackTimer += Time.deltaTime;

        if (!damageApplied && attackTimer >= attackTime * 0.5f)
        {   //play animation
            if (TargetInRange(brain.player))
            {
                IDamageable damagable = brain.player.GetComponent<IDamageable>();
                if (damagable!= null)
                {
                    damagable.TakeDamage(brain.attackDamage);
                }
                Debug.Log("[Attack] Damage Applied.");
                damageApplied = true;
            }
            //check if player is in range in order to apply damage
            //hit and apply damage to player
            Debug.Log("[Attack] Target out of range");

            //if attack animation is over and player is out of range then ignore cooldown... and start chasing
        }

        if (attackTimer >= attackTime)
        {
            brain.NotifyAttackEnded();
            brain.SwitchState(brain.ChaseState);
            Debug.Log("[Attack] Attack ended switching to Chase.");
        }
    }
    public void OnExit()
    {
        brain.enemyMovement.SetRotationPermission(true);
    }

    private bool TargetInRange(GameObject target)
    {
        var distance = Vector3.Distance(brain.transform.position, target.transform.position);
        return distance <= brain.attackDistance;
    }
}

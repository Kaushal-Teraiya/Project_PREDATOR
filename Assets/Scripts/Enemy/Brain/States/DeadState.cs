using UnityEngine;

public class DeadState : IEnemyState
{

    private EnemyBrain brain;
    public DeadState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        var Enemy = brain.enemyMovement;
        Enemy.Stop();
        Enemy.ClearRotationIntent();
        Enemy.SetRotationPermission(false);
        Enemy.SetMovementMode(EnemyMovement.MovementMode.Idle);
        Enemy.DisableProximity();
        brain.SetCurrentlyChasing(false);
        brain.DisablePerception();
        brain.GetComponentInChildren<CapsuleCollider>().enabled = false;
        // 
        EnableRagdoll();
        //Enable Ragdoll here
    }

    public void Tick()
    {

    }

    public void OnExit() { }

    public void EnableRagdoll()
    {
        if (brain.ragdollController == null)
        {
            Debug.Log("[DeadState] Ragdoll controller is null");
            brain.enemyMovement.Animator_SetTrigger("IsDead");
            return;
        }
          Vector3 testDirection =
            (brain.transform.position - brain.player.transform.position).normalized;
        brain.ragdollController.EnableFullRagdoll(testDirection, brain.LastHitForce);
    }
}

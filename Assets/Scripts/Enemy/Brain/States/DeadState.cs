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
        Enemy.Animator_SetTrigger("IsDead");
        EnableRagdoll();
        //Enable Ragdoll here
    }

    public void Tick()
    {
       
    }

    public void OnExit() { }

    public void EnableRagdoll()
    {
        //disable main colldier(capsule)
        //enable all body colliders and rigidbodies
    }
}

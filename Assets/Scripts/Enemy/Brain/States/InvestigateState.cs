
using UnityEngine;

public class InvestigateState : IEnemyState
{
    private EnemyBrain brain;
    public bool hasReachedDestination { get; private set; }
    private Vector3 areaCenter;
    private float areaRadius;

    public void SetAreaCenter_AreaRadius(Vector3 _areaCenter, float _areaRadius)
    {
        areaCenter = _areaCenter;
        areaRadius = _areaRadius;
    }

    public InvestigateState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void OnEnter()
    {
        brain.EnemyMovement.SetMovementMode(EnemyMovement.MovementMode.Investigate);
        hasReachedDestination = false;
//        Debug.Log("investigation starts");
        brain.EnemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, areaCenter);
    }

    public void Tick()
    {
        var Enemy = brain.EnemyMovement;
        Enemy.MoveTo(areaCenter);
        Vector3 moveDir = Enemy.GetMovementDirection();

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Enemy.RotationIntent(EnemyMovement.RotationPriority.State, brain.transform.position + moveDir);
        }
        //   Enemy.RotationIntent(EnemyMovement.RotationPriority.State, areaCenter);
        Enemy.RequestAnimation(new AnimationIntent(AnimationType.Walk, 20));
        float distance = Vector3.Distance(Enemy.transform.position, areaCenter);
        if (distance <= areaRadius)
        {
            hasReachedDestination = true;
        }
    }
    public void OnExit()
    {
//        Debug.Log("investigation exits");
        brain.EnemyMovement.ClearRotationIntent();
        // brain.enemyMovement.Stop();
    }
}

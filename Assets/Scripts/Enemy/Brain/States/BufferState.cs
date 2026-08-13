using UnityEngine;

public class BufferState : IEnemyState
{
    private EnemyBrain brain;

    public BufferState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        //brain._AttackRepositionStyle.DecideNextAction();
        var distance = Vector3.Distance(brain.transform.position, brain.player.transform.position);

        if (distance <= brain.AttackDistance)
        {
            brain.SwitchState(brain.AttackState);
        }
        else
        {
            brain.SwitchState(brain.ChaseState);
        }
    }

    public void Tick()
    {

    }

    public void OnExit()
    {

    }
}

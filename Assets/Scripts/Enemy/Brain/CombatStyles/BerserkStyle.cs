using UnityEngine;

public class BerserkStyle : ICombatStyle
{
    private EnemyBrain brain;

    public BerserkStyle(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void Enter()
    {
        Debug.Log("[Berserk Style ENTER]");

        // brain.SetAttackRegisterDistance(brain.CloseAttackAsset);
    }

    public void ExecuteOneShotDecesion()
    {
        Debug.Log("[Berserk Style OneShot]");
        Debug.Log("Berserk style");
        //brain.SelectCloseOrMidBand();
        float distance = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        if (distance <= brain.AttackRegisterDistance)
        {
            brain.SwitchState(brain.AttackState);
        }
        else
        {
            brain.SwitchState(brain.ChaseState);
        }
    }

    public void HandleAttackEnd()
    {
        Debug.Log("Berserk style");
        brain.SelectCloseOrMidBand();
        float distance = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        if (distance <= brain.AttackRegisterDistance)
        {
            brain.SwitchState(brain.AttackState);
        }
        else
        {
            brain.SwitchState(brain.ChaseState);
        }
    }

    public void Exit()
    {
        Debug.Log("[Berserk Style EXIT]");
    }

    public void Tick()
    {
        Debug.Log("[Berserk Style TICK]");
    }

    public void PrepareInitialDecision()
    {
        brain.SelectCloseOrMidBand();
    }

}

using UnityEngine;
public class CombatState : IEnemyState
{
    private EnemyBrain brain;

    public CombatState(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void OnEnter()
    {
        Debug.Log("Combat ENTER");
        brain.CurrentCombatStyle.Enter();
        brain.CurrentCombatStyle.ExecuteOneShotDecesion();
    }

    public void Tick()
    {
        brain.CurrentCombatStyle.Tick();
    }

    public void OnExit()
    {
        //combat state can exit when u hit a zombie check function "EnableSpeed()" inside EnemyHealth.cs
        brain.CurrentCombatStyle.Exit();
    }
}

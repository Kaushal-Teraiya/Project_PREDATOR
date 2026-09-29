using UnityEngine;

public interface ICombatStyle
{
    void PrepareInitialDecision();
    void ExecuteOneShotDecesion();
    void Enter();
    void Tick();
    void HandleAttackEnd();
    void Exit();
}

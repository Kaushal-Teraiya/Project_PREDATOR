using UnityEngine;

public interface ICombatStyle
{
    void ExecuteOneShotDecesion();
    void Enter();
    void Tick();
    void HandleAttackEnd();
    void Exit();
}

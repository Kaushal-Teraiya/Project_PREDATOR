using UnityEngine;

public interface IEnemyState
{
    void OnEnter();
    void Tick();
    void Exit();
}
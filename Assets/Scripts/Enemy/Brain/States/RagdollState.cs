using NUnit.Framework.Internal;
using UnityEngine;

public class RagdollState : IEnemyState
{
    private EnemyBrain brain;

    public RagdollState(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void OnEnter() { }
    public void Tick() { }
    public void OnExit() { }
}

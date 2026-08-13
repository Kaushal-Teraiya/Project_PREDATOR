using System;
using UnityEngine;

public static class CombatStyleFactory
{
    public static ICombatStyle Create(EnemyBrain.CombatStyleType combatStyleType, EnemyBrain brain)
    {
        switch (combatStyleType)
        {
            case EnemyBrain.CombatStyleType.AttackReposition:
                return new AttackRepositionStyle(brain);
            case EnemyBrain.CombatStyleType.Berserk:
                return new BerserkStyle(brain);
            case EnemyBrain.CombatStyleType.Circling:
                return new CirclingStyle(brain);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

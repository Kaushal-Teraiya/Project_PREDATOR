using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "EnemyArchetype/NewEnemyType")]
public class EnemyArchetype : ScriptableObject
{
   public List<EnemyAbility> enemyAbilities = new List<EnemyAbility>();
}

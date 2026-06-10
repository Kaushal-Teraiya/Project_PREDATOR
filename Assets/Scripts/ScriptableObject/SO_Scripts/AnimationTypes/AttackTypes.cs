using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/AttackProfile")]
public class AttackTypes : ScriptableObject
{
    public string attackName;
    public float minRange, maxRange;
    public int damage;
    public List<AnimationClip> attackAnimations;
    public float cooldown;
    public bool retreat;
    public bool usesLeap;
    public float leapArcHeight;
    public float leapDelay;
    public float leapDuration;
    public float attackRange;
    public float attackRegisterDistance;
}

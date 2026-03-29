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
    public bool usesLunge;
    public float lungeArcHeight;
    public float lungeDelay;
    public float lungeDuration;
}

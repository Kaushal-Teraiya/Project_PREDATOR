using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimationController : MonoBehaviour
{
    [Header("Animation Data")]
    //[SerializeField] private List<AnimationClip> attackVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> idleVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> walkVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> runVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> deathVariants = new List<AnimationClip>();
    [SerializeField] private List<AnimationClip> hitReactionVariants = new List<AnimationClip>();

    public AnimatorOverrideController animatorOverrideController;

    private Animator animator;
    private RuntimeAnimatorController baseController;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        baseController = animator.runtimeAnimatorController;

        animatorOverrideController = new AnimatorOverrideController
        {
            runtimeAnimatorController = baseController
        };

        AnimationClip walkVariant = walkVariants[UnityEngine.Random.Range(0, walkVariants.Count)];
        AnimationClip idleVariant = idleVariants[UnityEngine.Random.Range(0, idleVariants.Count)];
        AnimationClip runVariant = runVariants[UnityEngine.Random.Range(0, runVariants.Count)];
        //   AnimationClip attackVariant = attackVariants[UnityEngine.Random.Range(0, attackVariants.Count)];
        AnimationClip deathVariant = deathVariants[UnityEngine.Random.Range(0, deathVariants.Count)];
        AnimationClip hitReactionVariant = hitReactionVariants[UnityEngine.Random.Range(0, hitReactionVariants.Count)];

        animatorOverrideController["Walk_"] = walkVariant;
        animatorOverrideController["Idle_"] = idleVariant;
        animatorOverrideController["Run_"] = runVariant;
        // animatorOverrideController["Attack_"] = attackVariant;
        animatorOverrideController["Death_"] = deathVariant;
        animatorOverrideController["HitReaction_"] = hitReactionVariant;
        animator.runtimeAnimatorController = animatorOverrideController;
    }

    public void OverrideAttackAnimation(AnimationClip clip)
    {
        animatorOverrideController["Attack_"] = clip;
        animator.runtimeAnimatorController = animatorOverrideController;
        animator.Play("Attack", 0, 0f);
    }
}
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{

    [SerializeField] private int maxHealth;
    private EnemyBrain brain;
    private Coroutine hitReactionCoroutine;

    public float currentHealth { get; private set; }
    public bool EnemyisDead { get; private set; }

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(HitResult hitResult, float amount)
    {
        if (EnemyisDead)
        {
            this.StopAllCoroutines();
            return;
        }

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            EnemyisDead = true;
            EnemyDie(hitResult);
            return;
        }
        // 50% chance to play hit reaction
        if (Random.value < 0.5f)
        {
            brain.EnemyMovement.SetMovementMode(EnemyMovement.MovementMode.Idle);
            brain.EnemyMovement.Animator_SetTrigger("Hit");

            if (hitReactionCoroutine != null)
                StopCoroutine(hitReactionCoroutine);

            hitReactionCoroutine = StartCoroutine(EnableSpeed());
        }
    }

    private IEnumerator EnableSpeed()
    {
        // Wait until the animator actually enters the hit reaction state
        while (!brain.EnemyMovement._Animator.GetCurrentAnimatorStateInfo(0).IsName("HitReaction_"))
            yield return null;

        // Wait until the animation finishes
        while (brain.EnemyMovement._Animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;

        if (!EnemyisDead) brain.SwitchState(brain.BufferState);

        hitReactionCoroutine = null;
    }

    private void EnemyDie(HitResult hitResult)
    {
        brain.SetHitImpact(hitResult.hitDirection, hitResult.hitForce);
        brain.HandleDeath();
        //do other stuff on enemy death
        //Debug.Log("[EnemyHealth] Enemy is Dead.");
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Vector3 headPosition = transform.position + Vector3.up * 3.5f;

        if (EnemyisDead)
        {
            Gizmos.color = Color.red;
        }
        else if (currentHealth < 100 && currentHealth > 0)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            Gizmos.color = Color.green;
        }

        Gizmos.DrawSphere(headPosition, 0.2f);
    }

}

using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{

    [SerializeField] private int maxHealth;
    private EnemyBrain brain;
    public int currentHealth { get; private set; }
    public bool EnemyisDead { get; private set; }

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (EnemyisDead)
        {
            return;
        }

        currentHealth -= amount;
        Debug.Log("[EnemyHealth] Enemy is taking Damage.");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            EnemyisDead = true;
            EnemyDie();
        }
    }

    private void EnemyDie()
    {
        brain.HandleDeath();
        //do other stuff on enemy death
        Debug.Log("[EnemyHealth] Enemy is Dead.");
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

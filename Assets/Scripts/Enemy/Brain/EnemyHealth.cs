using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{

    [SerializeField] private int maxHealth;
    public int currentHealth { get; private set; }
    public bool EnemyisDead { get; private set; }

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
        //do other stuff on enemy death
        Debug.Log("[EnemyHealth] Enemy is Dead.");
    }


}

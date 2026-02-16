using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth;
    public int currentHealth { get; private set; }
    public bool playerisDead { get; private set; }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (playerisDead)
        {
            return;
        }

        currentHealth -= amount;
        Debug.Log("[PlayerHealth] player is taking Damage.");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            playerisDead = true;
            PlayerDie();
        }
    }

    private void PlayerDie()
    {
        //do other stuff on player death
        Debug.Log("[PlayerHealth] player is Dead.");
    }
}

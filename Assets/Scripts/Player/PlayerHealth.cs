using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private PlayerCombat playerCombat;
    public int currentHealth { get; private set; }
    public bool playerisDead { get; private set; }

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerLook = GetComponent<PlayerLook>();
    }

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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerMovement.DisableMovement();
        playerLook.DisableLook();
        playerCombat.DisableCombat();
        Debug.Log("[PlayerHealth] player is Dead.");
    }
}

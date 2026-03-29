using System;
using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private PlayerCombat playerCombat;
    public int currentHealth { get; private set; }
    public bool playerisDead { get; private set; }
    private TextMeshProUGUI healthText;
    public event Action<bool> playerDead;

    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerLook = GetComponent<PlayerLook>();
        healthText = GetComponentInChildren<TextMeshProUGUI>();
    }

    void Update()
    {
        if (healthText != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (playerisDead)
        {
            playerDead?.Invoke(true);
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

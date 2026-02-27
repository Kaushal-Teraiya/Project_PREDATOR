using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private int damageAmount;
    public int DamageAmount => damageAmount;
    public InputActionReference attack;
    [SerializeField] private float attackRange;

    void Start()
    {
        attack.action.Enable();
    }

    void Update()
    {
        if (attack.action.WasPressedThisFrame())
        {
            Camera cam = Camera.main;
            Vector3 rayOrigin = cam.transform.position + Vector3.forward * 0.5f;
            Vector3 direction = cam.transform.forward.normalized;
            RaycastHit HitInfo;
            Debug.DrawRay(rayOrigin, direction * attackRange, Color.green, 1f);

            if (Physics.Raycast(rayOrigin, direction, out HitInfo, attackRange))
            {
                IDamageable damageable = HitInfo.collider.GetComponentInParent<IDamageable>();

                if (damageable != null && damageable != transform.GetComponentInParent<IDamageable>())
                {
                    Debug.Log("[PlayerCombat] Applying Damage to Enemies.");
                    damageable.TakeDamage(damageAmount);
                }
                else
                {
                    Debug.Log("[playerCombat] damageable is NUll");
                }
            }
        }
    }

    public void DisableCombat()
    {
        attack.action.Disable();
    }
}

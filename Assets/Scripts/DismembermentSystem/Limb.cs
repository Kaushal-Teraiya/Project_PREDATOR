using UnityEngine;

public class Limb : MonoBehaviour
{
    private void Start()
    {
        LimbRagdollSetup ragdoll = gameObject.AddComponent<LimbRagdollSetup>();
        ragdoll.CreateRagdoll();
    }
}
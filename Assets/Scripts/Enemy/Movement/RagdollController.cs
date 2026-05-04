using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class RagdollController : MonoBehaviour
{
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;

    private Rigidbody rootRigidbody;
    private CapsuleCollider mainCapsule;
    private Animator animator;
    private NavMeshAgent agent;
    public Rigidbody hips;

    public bool IsRagdollActive { get; private set; }

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        mainCapsule = GetComponentInChildren<CapsuleCollider>();
        rootRigidbody = GetComponent<Rigidbody>();

        CacheRagdollParts();
        DisableRagdoll();
    }

    void CacheRagdollParts()
    {
        List<Rigidbody> bodies = new List<Rigidbody>();
        List<Collider> colliders = new List<Collider>();

        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb == rootRigidbody)
                continue;

            bodies.Add(rb);
        }

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            if (col == mainCapsule)
                continue;

            if (col.isTrigger)
                continue;

            colliders.Add(col);
        }

        ragdollRigidbodies = bodies.ToArray();
        ragdollColliders = colliders.ToArray();
    }

    public void EnableFullRagdoll(Vector3 forceDirection, float forceAmount)
    {
        if (IsRagdollActive)
            return;

        IsRagdollActive = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
        animator.enabled = false;
        agent.enabled = false;
        mainCapsule.enabled = false;
       // rootRigidbody.isKinematic = true;
        foreach (var col in ragdollColliders)
            col.enabled = true;

        foreach (var rb in ragdollRigidbodies)
            rb.isKinematic = false;

        Physics.SyncTransforms();

        ApplyForce(forceDirection, forceAmount);
    }

    public void DisableRagdoll()
    {
        IsRagdollActive = false;

        foreach (var rb in ragdollRigidbodies)
            rb.isKinematic = true;

        foreach (var col in ragdollColliders)
            col.enabled = false;

        animator.enabled = true;
        agent.enabled = true;
        mainCapsule.enabled = true;
        //rootRigidbody.isKinematic = false;
    }

    void ApplyForce(Vector3 direction, float amount)
    {

        if (hips == null)
            return;

        hips.AddForce(direction * amount, ForceMode.Impulse);

    }
}
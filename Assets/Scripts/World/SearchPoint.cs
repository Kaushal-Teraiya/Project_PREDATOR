using System;
using System.Collections.Generic;
using Unity.VisualScripting;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class SearchPoint : MonoBehaviour
{
    [SerializeField] private Transform interactionAnchor;
    public Transform InteractionAnchor => interactionAnchor;
    private EnemyBrain currentOccupant;
    public bool IsOccupied => currentOccupant != null;
    public float arrivalRadius { get; private set; } = 0.1f;
    public float searchPointRadius { get; private set; } = 5f;
    [SerializeField] private int slotCount = 6;
    [SerializeField] private float zombieBodyRadius = 0.35f;
    private float standingRadius;
    [SerializeField] private float radiusNoise;
    [SerializeField] private LayerMask obstacleMask;
    private List<AngularSlots> angularSlotsList = new List<AngularSlots>();

    private struct AngularSlots
    {
        public float startAngle, endAngle;
        public bool isBlocked, isOccupied;
        public float radius;

    }


    public void GenerateSlots()
    {
        AngularSlots angularSlots;
        angularSlots.startAngle = angularSlots.endAngle = 0f;
        angularSlots.isBlocked = angularSlots.isOccupied = false;
        // CalculateStandRadius();
        ResetSlots();
        float slotAngleSize = 360f / slotCount;
        for (int i = 0; i < slotCount; i++)
        {
            angularSlots.startAngle = i * slotAngleSize;
            angularSlots.endAngle = angularSlots.startAngle + slotAngleSize;
            float min, max;
            min = 2.5f;
            max = searchPointRadius;
            angularSlots.radius = UnityEngine.Random.Range(min, max);
            angularSlots.isBlocked = false;
            angularSlots.isOccupied = false;
            angularSlotsList.Add(angularSlots);
        }

        ValidateSlots(angularSlotsList);
        //validate slots here
    }

    private void ValidateSlots(List<AngularSlots> angularSlots)
    {
        for (int i = 0; i < angularSlotsList.Count; i++)
        {
            float slotMidAngle = (angularSlots[i].startAngle + angularSlots[i].endAngle) * 0.5f * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(Mathf.Cos(slotMidAngle), 0f, Mathf.Sin(slotMidAngle));
            float radius  =angularSlotsList[i].radius;
            Vector3 candidatePosition = transform.position + direction * radius;
            candidatePosition += Vector3.up * 0.5f;
            Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
            Vector3 toCandidate = candidatePosition - transform.position;
            float rayDistance = toCandidate.magnitude - zombieBodyRadius;

            bool overlapsWall = Physics.OverlapSphere(candidatePosition, zombieBodyRadius, obstacleMask).Length > 0;
            bool pathBlocked = Physics.Raycast(rayOrigin, toCandidate.normalized, rayDistance, obstacleMask);

            AngularSlots slot = angularSlotsList[i];
            slot.isBlocked = overlapsWall || pathBlocked;
            angularSlotsList[i] = slot;
        }

    }

    public int TryClaimObserverSlot()
    {
        for (int i = 0; i < angularSlotsList.Count; i++)
        {
            if (!angularSlotsList[i].isBlocked && !angularSlotsList[i].isOccupied)
            {
                Debug.Log("GENERATED SLOTS");
                AngularSlots slot = angularSlotsList[i];
                slot.isOccupied = true;
                angularSlotsList[i] = slot;
                return i;
            }

        }
        Debug.Log("NO SLOTS");
        return -1;

    }

    public void ReleaseObserverSlot(int index)
    {
        AngularSlots slot = angularSlotsList[index];
        slot.isOccupied = false;
        angularSlotsList[index] = slot;
    }

    public Vector3 GetObserverPosition(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= angularSlotsList.Count)
        {
            return transform.position;
        }

        AngularSlots slot = angularSlotsList[slotIndex];
        float randomAngle = UnityEngine.Random.Range(slot.startAngle, slot.endAngle) * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
        Vector3 center = transform.position;
        float radialNoise = UnityEngine.Random.Range(-radiusNoise, radiusNoise);
        float finalRadius = Mathf.Max(0.2f, slot.radius + radialNoise);
        Vector3 offset = direction * finalRadius;
        Vector3 position = center + offset;
        return position;
    }

    private void ResetSlots()
    {
        angularSlotsList.Clear();
    }

    public void ReleaseSearchPoint(EnemyBrain requester)
    {
        if (currentOccupant != requester)
        {
            Debug.Log($"{requester.name} tried to release {name} but is not occupant");
            return;
        }
        Debug.Log($"{requester.name} RELEASED {name}");
        SetArrivalRadius(0.3f);
        currentOccupant = null;
    }

    public bool TryClaimSearchPoint(EnemyBrain requester)
    {
        if (IsOccupied)
        {
            Debug.Log($"{requester.name} FAILED to claim {name} (occupied by {currentOccupant.name})");
            return false;
        }

        if (requester == null)
        {
            return false;
        }


        if (!requester.CanClaim(this))
        {
            return false;
        }

        currentOccupant = requester;
        SetArrivalRadius(10f);

        Debug.Log($"{requester.name} CLAIMED {name}");
        return true;

    }

    public void SetArrivalRadius(float _changedArrivalradius)
    {
        arrivalRadius = _changedArrivalradius;
    }

    public bool IsCenterBlockedFor(EnemyBrain requester)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.8f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") &&
                hit.transform != requester.transform)
            {
                return true;
            }
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (searchPointRadius <= 0f)
            return;

        Gizmos.color = Color.hotPink;
        DrawCircle(
            transform.position,
            searchPointRadius,
            40
        );
    }

    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector3 nextPoint = center + new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius
            );

            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (angularSlotsList == null || angularSlotsList.Count == 0)
            return;

        Vector3 center = transform.position;

        foreach (var slot in angularSlotsList)
        {
            // Choose color
            if (slot.isBlocked)
                Gizmos.color = Color.red;
            else if (slot.isOccupied)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.green;

            // Draw start angle line
            DrawSlotLine(center, slot.startAngle, slot.radius);

            // Draw end angle line
            DrawSlotLine(center, slot.endAngle, slot.radius);

            // Optional: draw midpoint indicator
            float midAngle = (slot.startAngle + slot.endAngle) * 0.5f;
            DrawSlotPoint(center, midAngle, slot.radius * 0.95f);
        }
    }

    void DrawSlotLine(Vector3 center, float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        Gizmos.DrawLine(center, center + dir * radius);
    }

    void DrawSlotPoint(Vector3 center, float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 pos = center + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
        Gizmos.DrawSphere(pos, 0.08f);
    }




}

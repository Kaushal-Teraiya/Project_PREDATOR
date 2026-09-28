using UnityEngine;

[RequireComponent(typeof(EnemyBrain))]
public class EnemyDebugGizmos : MonoBehaviour
{
    private EnemyBrain brain;

    private void OnDrawGizmos()
    {
        brain ??= GetComponent<EnemyBrain>();
        if (brain == null || brain.player == null) return;

        Vector3 player = brain.player.transform.position;

        // Combat bands
        DrawBand(brain.CloseAttackAsset, player, Color.green);
        DrawBand(brain.MidAttackAsset, player, Color.yellow);
        DrawBand(brain.FarAttackAsset, player, Color.red);

        // Investigation
        if (brain.CurrentInvestigationRadius > 0f)
        {
            Gizmos.color = Color.orange;
            DrawCircle(brain.CurrentInvestigationCenter,
                brain.CurrentInvestigationRadius, 40);
        }

        // Chase target
        if (brain.chaseTargetPosition != Vector3.zero)
        {
            Gizmos.color = brain.chaseTargetPosition == transform.position
                ? Color.green : Color.red;

            Gizmos.DrawSphere(brain.chaseTargetPosition, .25f);
            Gizmos.DrawLine(transform.position, brain.chaseTargetPosition);
        }

        Gizmos.color = Color.brown;
        Gizmos.DrawLine(transform.position,
            transform.position + Vector3.up * 2f);

        DrawRepositionTarget();

        // Engagement radius
        if (brain.FarAttackAsset != null)
        {
            float radius = brain.FarAttackAsset.maxRange * 1.5f;
            Gizmos.color = new Color(1f, 0f, 1f, .9f);

            for (int i = 0; i < 5; i++)
                DrawCirclev2(player, radius + .15f * (i - 2.5f));
        }

        // Lunge prediction
        if (brain.FarAttackAsset != null && brain.PlayerMovement != null)
        {
            float time = brain.FarAttackAsset.leapDelay +
                         brain.FarAttackAsset.leapDuration;

            Vector3 predicted = brain.PredictPlayerPosition(time, 2.5f);
            Vector3 direction = (predicted - transform.position).normalized;
            Vector3 landing = predicted -
                              direction *
                              (brain.FarAttackAsset.minRange - 1.2f);

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(player, .15f);

            Gizmos.color = Color.orangeRed;
            Gizmos.DrawLine(player, predicted);

            Gizmos.color = Color.pink;
            Gizmos.DrawSphere(predicted, .18f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(landing, .22f);
            Gizmos.DrawLine(transform.position, landing);
        }

        DrawMovementDebug(player);

        if (brain.FarAttackAsset == null) return;

        float r = brain.FarAttackAsset.maxRange + 1.5f;

        if (brain.Debug_FrontSemiCircle)
            DrawSemicircle(player,
                brain.Debug_movementAngle - brain.SliceHalfAngle,
                brain.Debug_movementAngle + brain.SliceHalfAngle,
                r, Color.green);

        if (brain.Debug_BackSemiCircle)
        {
            float angle = brain.Debug_movementAngle + 180f;

            DrawSemicircle(player,
                angle - brain.SliceHalfAngle,
                angle + brain.SliceHalfAngle,
                r, Color.red);
        }
    }

    private void DrawBand(AttackTypes profile, Vector3 center, Color color)
    {
        if (profile == null) return;

        Gizmos.color = color;
        DrawCirclev2(center, profile.minRange);
        DrawCirclev2(center, profile.maxRange);
    }

    private void DrawCirclev2(Vector3 center, float radius)
    {
        float step = Mathf.PI * 2f / brain.CircleSegments;
        Vector3 previous = center + Vector3.right * radius;

        for (int i = 1; i <= brain.CircleSegments; i++)
        {
            float angle = step * i;
            Vector3 next = center +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }

    private void DrawSemicircle(
        Vector3 center, float start, float end,
        float radius, Color color)
    {
        Gizmos.color = color;

        const int segments = 40;
        const float thickness = 1.2f;
        float step = (end - start) / segments;

        for (int i = 0; i < segments; i++)
        {
            Vector3 innerA = center + Direction(start + step * i) * radius;
            Vector3 innerB = center + Direction(start + step * (i + 1)) * radius;
            Vector3 outerA = center + Direction(start + step * i) *
                             (radius + thickness);
            Vector3 outerB = center + Direction(start + step * (i + 1)) *
                             (radius + thickness);

            Gizmos.DrawLine(innerA, innerB);
            Gizmos.DrawLine(outerA, outerB);
            Gizmos.DrawLine(innerA, outerA);
        }
    }

    private Vector3 Direction(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
    }

    private void DrawMovementDebug(Vector3 player)
    {
        if (brain.PlayerMovement == null) return;

        Vector3 velocity = brain.PlayerMovement.GetPlayerVelocity();
        velocity.y = 0f;

        if (velocity.sqrMagnitude < .01f) return;

        Vector3 direction = velocity.normalized;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(player, player + direction * 5f);

        Vector3 perpendicular = Vector3.Cross(Vector3.up, direction);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(
            player - perpendicular * 5f,
            player + perpendicular * 5f);
    }

    private void DrawRepositionTarget()
    {
        if (brain.Debug_RepositionTarget == Vector3.zero) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(brain.Debug_RepositionTarget, 1f);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float step = 360f / segments;
        Vector3 previous = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = step * i;

            Vector3 next = center + new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius);

            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
using TMPro;
using UnityEngine;

public class CirclingStyle : ICombatStyle
{
    private float circlingRadius = 15f;
    private float orbitingSpeed = 50f;
    private Vector3 currentPosition;
    private EnemyBrain brain;
    private float orbitAngle;

    public CirclingStyle(EnemyBrain brain)
    {
        this.brain = brain;
    }
    public void Enter()
    {
        Debug.Log("Circling state entered");

        brain.EnemyMovement.SetMovementMode(
            EnemyMovement.MovementMode.Chase);

        brain.EnemyMovement.SetPlayerAvoidance(false);

        Vector3 offset =
            brain.transform.position -
            brain.player.transform.position;

        orbitAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
    }

    public void ExecuteOneShotDecesion()
    {
        Debug.Log("[CirclingStyle] One Shot Executed.");
    }

    public void HandleAttackEnd()
    {
        Debug.Log("[CirclingStyle] Handling Attack End");
        brain.SwitchState(brain.CombatState);
    }

    public void Exit()
    {
        Debug.Log("Exited Circling Style");
    }

    public void Tick()
    {
        brain.EnemyMovement.RequestAnimation(new AnimationIntent(AnimationType.Run, 50));

        Vector3 center = brain.player.transform.position;

        // Move around the player by advancing our orbit angle.
        orbitAngle -= orbitingSpeed * Time.deltaTime;

        float radians = orbitAngle * Mathf.Deg2Rad;

        Vector3 targetPosition = center + new Vector3(Mathf.Cos(radians) * circlingRadius, 0f, Mathf.Sin(radians) * circlingRadius);

        brain.EnemyMovement.RotationIntent(EnemyMovement.RotationPriority.Proximity, targetPosition);
        brain.EnemyMovement.MoveTo(targetPosition);
    }

}

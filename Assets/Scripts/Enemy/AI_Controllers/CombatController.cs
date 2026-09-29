using UnityEngine;

public class CombatController
{
    private readonly EnemyBrain brain;
    private readonly PlayerMovement playerMovement;

    public CombatController(EnemyBrain brain, PlayerMovement playerMovement)
    {
        this.brain = brain;
        this.playerMovement = playerMovement;
    }

    public void SelectNextBand()
    {
        var value = UnityEngine.Random.value;

        if (value < 0.33f)
        {
            brain.SetCombatBand(EnemyBrain.CombatBand.Close, brain.CloseAttackAsset);
        }
        else if (value < 0.66f)
        {
            brain.SetCombatBand(EnemyBrain.CombatBand.Mid, brain.MidAttackAsset);
        }
        else
        {
            brain.SetCombatBand(EnemyBrain.CombatBand.Far, brain.FarAttackAsset);
        }
    }

    public void SelectCloseOrMidBand()
    {
        if (UnityEngine.Random.value < 0.5f)
            brain.SetCombatBand(EnemyBrain.CombatBand.Close, brain.CloseAttackAsset);
        else
            brain.SetCombatBand(EnemyBrain.CombatBand.Mid, brain.MidAttackAsset);
    }

    public Vector2 GetEffectiveBandRange(AttackTypes attackProfile)
    {
        var effectiveMinRange = attackProfile.minRange;
        var effectiveMaxRange = attackProfile.maxRange;

        if (playerMovement.isPerformingAction)
        {
            if (attackProfile == brain.MidAttackAsset)
            {
                effectiveMaxRange *= brain.MidBandCompression;
                effectiveMinRange *= brain.MidBandCompression;
            }
            else if (attackProfile == brain.FarAttackAsset)
            {
                effectiveMaxRange *= brain.FarBandCompression;
                effectiveMinRange *= brain.FarBandCompression;
            }
        }

        float safetyMargin = 0.4f;

        if (effectiveMaxRange < effectiveMinRange + safetyMargin)
            effectiveMaxRange = effectiveMinRange + safetyMargin;

        return new Vector2(effectiveMinRange, effectiveMaxRange);
    }

    public bool IsInCombatBand()
    {
        // if (brain.CurrentAttackProfile == null)
        //     return false;

        float distanceToPlayer = Vector3.Distance(brain.transform.position, brain.player.transform.position);
        float desiredBandDistance = GetDesiredBandDistance(brain.GetCurrentCombatBand(), brain.CurrentAttackProfile) / 2f;
        float positioning = Mathf.Abs(distanceToPlayer - desiredBandDistance);

        return positioning <= desiredBandDistance + brain.BandDistanceTolerance;
    }

    private float GetDesiredBandDistance(EnemyBrain.CombatBand combatBand, AttackTypes currentProfile)
    {
        if (combatBand == EnemyBrain.CombatBand.Far && currentProfile == brain.FarAttackAsset)
            return currentProfile.attackRange;
        else if (combatBand == EnemyBrain.CombatBand.Mid && currentProfile == brain.MidAttackAsset)
            return currentProfile.attackRange;
        else
            return currentProfile.attackRange;
    }

    public Vector3 PredictPlayerPosition(float predictionTime, float maxPredictionDistance)
    {
        var playerVelocity = playerMovement.GetPlayerVelocity();
        playerVelocity.y = 0f;

        if (playerVelocity.sqrMagnitude < 0.01f)
            return brain.player.transform.position;

        Vector3 predictedOffset = playerVelocity * predictionTime;

        if (predictedOffset.magnitude > maxPredictionDistance)
            predictedOffset = predictedOffset.normalized * maxPredictionDistance;

        return brain.player.transform.position + predictedOffset;
    }

    public float ChooseBandSlice()
    {
        if (brain.CurrentAttackProfile == brain.FarAttackAsset)
            return UnityEngine.Random.Range(0f, 360f);

        var velocity = playerMovement.GetPlayerVelocity();
        velocity.y = 0f;

        if (velocity.sqrMagnitude < 0.01f)
            return UnityEngine.Random.Range(0f, 360f);

        var movementDirection = velocity.normalized;
        var movementIntent = Vector3.Dot(brain.player.transform.forward, movementDirection);
        Vector3 facing = brain.player.transform.forward;

        float movementAngle = Mathf.Atan2(facing.z, facing.x) * Mathf.Rad2Deg;

        brain.Debug_movementAngle = movementAngle;
        brain.Debug_FrontSemiCircle = brain.Debug_BackSemiCircle = false;

        if (movementIntent > 0.3f)
        {
            brain.Debug_FrontSemiCircle = true;
            return UnityEngine.Random.Range(movementAngle - brain.SliceHalfAngle, movementAngle + brain.SliceHalfAngle);
        }
        else if (movementIntent < -0.3f)
        {
            brain.Debug_BackSemiCircle = true;
            float oppositeAngle = movementAngle + 180f;
            return UnityEngine.Random.Range(oppositeAngle - brain.SliceHalfAngle, oppositeAngle + brain.SliceHalfAngle);
        }
        else
        {
            return UnityEngine.Random.Range(0f, 360f);
        }
    }

    public bool IsInCooldown()
    {
        return Time.time - brain.GetLastAttackEndTime() < brain.AttackCooldown;
    }

    public void NotifyAttackEnded()
    {
        brain.SetLastAttackEndTime(Time.time);
    }

    public void SetAttackRegisterDistance(AttackTypes currentProfile)
    {
        brain.SetAttackRegisterDistanceInternal(currentProfile.attackRegisterDistance);
    }
}
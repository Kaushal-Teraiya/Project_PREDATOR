using UnityEngine;

public class PerceptionController
{
    private readonly EnemyBrain brain;
    private readonly SoundSensor soundSensor;
    private readonly VisionSensor visionSensor;

    public Vector3 LastStimulusPosition { get; private set; }
    public float LastStimulusTime { get; private set; }
    public Vector3 LastConfirmedPosition { get; private set; }
    public float LastConfirmedSeenTime { get; private set; }
    public VisionSensor.visibilityResult PreviousResult { get; private set; }
    public VisionSensor.visibilityResult CurrentResult { get; private set; }
    public Vector3 SnapshotPosition { get; private set; }

    public PerceptionController(EnemyBrain brain, SoundSensor soundSensor, VisionSensor visionSensor)
    {
        this.brain = brain;
        this.soundSensor = soundSensor;
        this.visionSensor = visionSensor;
    }

    public void Initialize()
    {
        soundSensor.OnSoundHeard += HandleSoundStimulus;
        visionSensor.OnPeripheralGlimpse += HandlePeripheralStimulus;
    }

    public void Tick()
    {
        if (visionSensor != null)
        {
            CurrentResult = visionSensor.VisibilityResult;

            if (HasVision() && IsCenterVision())
            {
                LastConfirmedPosition = visionSensor.LastSeenPosition;
                LastConfirmedSeenTime = visionSensor.LastSeenTime;
            }

            //Only take 1 snapshot of playerposition for investigation on transition from none type of visibility into investigate  visibility
            if (CurrentResult == VisionSensor.visibilityResult.Investigate && PreviousResult == VisionSensor.visibilityResult.None)
            {
                SnapshotPosition = visionSensor.LastSeenPosition;
            }

            PreviousResult = CurrentResult;
        }

        Vector3 origin = visionSensor.transform.position;
        Vector3 direction = brain.player.transform.position - visionSensor.transform.position;
        float distance = direction.magnitude;
        //  bool recentlyChasing = currentlyChasing || Time.time - lastChaseTime <= visionGraceDuration;
        RaycastHit hit;

        if (distance < brain.ProximityRadius && !HasVision() && !brain.IsInState(brain.AttackState)) //this means even if we are not in enemy's vision it can still sense us if we are near them
        {
            if (!Physics.Raycast(origin, direction.normalized, out hit, distance, brain.ProximityObstacleMask))
            {
                brain.EnemyMovement.RotationIntent(EnemyMovement.RotationPriority.Proximity, origin + direction.normalized);
            }
            //later we can add closest player for multiplayer here
            // Debug.Log(hit.collider.gameObject.layer);
        }
    }

    public bool HasVision()
    {
        return visionSensor.HasLineOfSight;
    }

    public bool IsCenterVision()
    {
        return visionSensor.AngleFactor > 0.5f;
    }

    public bool CheckVisibilityResult(VisionSensor.visibilityResult expectedResult)
    {
        return visionSensor != null && visionSensor.VisibilityResult == expectedResult;
    }

    public bool WasRecentlyChasing()
    {
        return Time.time - LastConfirmedSeenTime <= brain.VisionGraceDuration;
    }

    public void ClearStimulus()
    {
        LastStimulusPosition = Vector3.zero;
    }

    public void DisablePerception()
    {
        visionSensor.enabled = false;
        soundSensor.enabled = false;
    }

    private void HandleSoundStimulus(Vector3 position)
    {
        LastStimulusPosition = position;
        LastStimulusTime = Time.time;
    }

    private void HandlePeripheralStimulus(Vector3 position)
    {
        LastStimulusPosition = position;
        LastStimulusTime = Time.time;
        brain.EnemyMovement.RotationIntent(EnemyMovement.RotationPriority.State, position);
    }
}

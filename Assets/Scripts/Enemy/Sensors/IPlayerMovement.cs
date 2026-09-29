public interface IPlayerMovement
{
    bool IsPerformingAction { get; }
    UnityEngine.Vector3 LastMovementDirection { get; }
}
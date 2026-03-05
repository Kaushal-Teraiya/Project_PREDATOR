public enum AnimationType
{
    None,
    Idle,
    Walk,
    Run,
    Attack,
    Death
}

public struct AnimationIntent
{
    public AnimationType animationType;
    public int priority;

    public AnimationIntent(AnimationType type, int priority)
    {
        animationType = type;
        this.priority = priority;
    }

}


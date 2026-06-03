public enum AnimationType
{
    None,
    Idle,
    Walk,
    Run,
    SurfaceCrawl,
    CrawlJump,
    Reposition,
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


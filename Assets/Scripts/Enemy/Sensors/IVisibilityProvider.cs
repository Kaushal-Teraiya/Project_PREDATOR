public interface IVisibilityProvider
{
    float GetVisibility();
    float BaseVisibility { get; }
    bool IsInDarkEnvironment { get; }
}
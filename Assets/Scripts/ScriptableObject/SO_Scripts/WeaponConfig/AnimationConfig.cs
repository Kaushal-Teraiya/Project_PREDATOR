using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/NewAnimationConfig")]
public class AnimationConfig : ScriptableObject
{
    public Animation reloadAnimation;
    public Animation idleAnimation;
    public Animation shootAnimation;
    public Animation walkAnimation;
    public Animation RunAnimation;
    public Animation equipAnimation;
    public Animation changeAnimation;
    public Animation recoilAnimation;

}

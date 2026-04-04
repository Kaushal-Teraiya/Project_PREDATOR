using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/Weapon/NewTrailConfig")]
public class TrailConfig : ScriptableObject
{
    public GameObject trailPrefab;
    public Material material;
    public AnimationCurve widthCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    public float duration = 0.5f;
    public float minVertexDistance = 0.1f;
    public Gradient Color;
    public float missDistance = 100f;
    public float trailSpeed = 100f;
}

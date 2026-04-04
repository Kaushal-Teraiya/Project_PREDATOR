using UnityEngine;

public class BulletTrails : MonoBehaviour
{
    private TrailRenderer trailRenderer;

    void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
    }
    public void Initialize(TrailConfig trailConfig)
    {
        trailRenderer.Clear();
        trailRenderer.material = trailConfig.material;
        trailRenderer.widthCurve = trailConfig.widthCurve;
        trailRenderer.time = trailConfig.duration;
        trailRenderer.minVertexDistance = trailConfig.minVertexDistance;
        trailRenderer.colorGradient = trailConfig.Color;
    }

    public void Clear()
    {
        trailRenderer.Clear();
    }

}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class PlayerVisiblity : MonoBehaviour , IVisibilityProvider
{
    private List<LightZone> activeLights = new List<LightZone>();
    private float visibility;
    [SerializeField] private float baseVisibility_UseLightZone = 0.15f;
    public float BaseVisibility => baseVisibility_UseLightZone;
    [SerializeField] private float baseVisibility_NoLightzone = 1f;
    [SerializeField] private float intensityTuner = 5f;
    [SerializeField] private bool useLightZone = true;
    public bool UseLightZone => useLightZone;
  
    private void OnTriggerEnter(Collider other)
    {
        var light = other.GetComponentInChildren<LightZone>();

        if (light != null)
        {
            RegisterLight(light);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var light = other.GetComponentInChildren<LightZone>();

        if (light != null)
        {
            UnregisterLight(light);
        }
    }

    private void RegisterLight(LightZone light)
    {
        if (!activeLights.Contains(light))
        {
            activeLights.Add(light);
        }
    }

    private void UnregisterLight(LightZone light)
    {
        activeLights.Remove(light);
    }

    public float GetVisibility()
    {
        if (useLightZone)
        {
            visibility = baseVisibility_UseLightZone;
        }
        else
        {
            visibility = baseVisibility_NoLightzone;
        }


        foreach (var light in activeLights)
        {
            var selectedLight = light.LightSource;
            var lightIntensity = selectedLight.intensity;
            var lightRange = selectedLight.range;

            var distance = Vector3.Distance(transform.position, selectedLight.transform.position);
            var distanceFactor = 1 - (distance / lightRange);
            distanceFactor = Mathf.Clamp01(distanceFactor);

            var normalizedIntensity = lightIntensity / (lightIntensity + intensityTuner);

            var contribution = normalizedIntensity * distanceFactor;
            visibility = Mathf.Max(visibility, contribution);
        }

        return visibility;
    }

    void Update()
    {
        // Debug.Log("[PlayerVisiblity] active lights: " + activeLights.Count);
//        Debug.Log("[PlayerVisibility] Visibility value is : " + GetVisiblity());
    }
}


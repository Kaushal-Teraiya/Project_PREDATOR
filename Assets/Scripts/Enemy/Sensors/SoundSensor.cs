using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundSensor : MonoBehaviour
{
    [Header("Hearing")]
    public float hearingMultiplier = 1f;
    
    [Header("Sound Memory (Read Only)")]
    public Vector3 LastHeardPosition { get; private set; }
    public float LastHeardRadius { get; private set; }
    public float LastHeardTime { get; private set; }
    public bool HasHeardSound { get; private set; }
    public int lastHeardValue { get; private set; }
    private int recentlyHeardValue = -1;
    [SerializeField] private float minSoundInterval = 0.2f;
    public event Action<Vector3> OnSoundHeard;

    public void ProcessSound(SoundEvent soundEvent)
    {
        if (Time.time - LastHeardTime < minSoundInterval)
            return;

        float distanceFromZombieToSoundSource = Vector3.Distance(transform.position, soundEvent.position);
        float hearingCapacity = soundEvent.radius * hearingMultiplier;

        if (distanceFromZombieToSoundSource > hearingCapacity)
            return;

        LastHeardRadius = soundEvent.radius;

        if (soundEvent.value >= recentlyHeardValue)
        {
            lastHeardValue = soundEvent.value;
            recentlyHeardValue = soundEvent.value;
            // Flatten to ground level
            LastHeardPosition = new Vector3(
                soundEvent.position.x,
                transform.position.y,
                soundEvent.position.z
            );

            LastHeardTime = Time.time;
            HasHeardSound = true;
            OnSoundHeard?.Invoke(LastHeardPosition);
        }
        //  Debug.Log($"{name} heard sound at {LastHeardPosition}");
    }

    public bool HasValidSound()
    {
        return HasHeardSound;
    }

    public void ClearSound()
    {
        HasHeardSound = false;
        recentlyHeardValue = -1;
        lastHeardValue = -1;
        LastHeardPosition = Vector3.zero;
        LastHeardRadius = 0f;
        LastHeardTime = 0f;

        Debug.Log("Sound cleared");
    }

    private void OnEnable()
    {
        SoundSystem.Register(this);
    }

    private void OnDisable()
    {
        SoundSystem.Unregister(this);
    }
}

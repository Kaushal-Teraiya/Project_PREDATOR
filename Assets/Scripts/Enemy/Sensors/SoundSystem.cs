public static class SoundSystem
{
    private static readonly System.Collections.Generic.HashSet<SoundSensor> listeners = new();

    public static void Register(SoundSensor sensor) => listeners.Add(sensor);
    public static void Unregister(SoundSensor sensor) => listeners.Remove(sensor);

    public static void Emit(SoundEvent soundEvent)
    {
        foreach (SoundSensor sensor in listeners)
            sensor.ProcessSound(soundEvent);
    }
}
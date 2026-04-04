using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundProfile" , menuName = "Sound/Sound Source Profile")]
public class SoundSource : ScriptableObject
{
    [SerializeField] private float radius;
    [SerializeField] private int value;

    public float Radius => radius;
    public int Value => value;
}

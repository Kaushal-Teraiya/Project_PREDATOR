using UnityEngine;

public class LightZone : MonoBehaviour
{
    [SerializeField] private Light lightSource;
    public Light LightSource => lightSource;
}

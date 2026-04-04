using Unity.Mathematics;
using UnityEngine;

public class WeaponCamera : MonoBehaviour
{
    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(Camera.main.transform.eulerAngles.x , transform.eulerAngles.y , 0f);
    }
}

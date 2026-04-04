using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/NewWeaponSoundConfig")]
public class WeaponSoundConfig : ScriptableObject
{
    public string weaponName, weaponType;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip recoilSound;
}

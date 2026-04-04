using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/NewWeaponVisualConfig")]
public class WeaponVisualConfig : ScriptableObject
{
    public string weaponName, weaponType;
    public GameObject weaponModel;
    public GameObject muzzleFlash;
    public Transform fireOriginOverride;
    public WeaponSoundConfig weaponSoundConfig;
    public TrailConfig trailConfig;

}

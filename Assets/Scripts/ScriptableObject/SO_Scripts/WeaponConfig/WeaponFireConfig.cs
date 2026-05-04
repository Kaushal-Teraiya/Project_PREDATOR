using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/NewWeaponFireConfig")]
public class WeaponFireConfig : ScriptableObject
{
    public string weaponName, weaponType;
    public float damage;
    public int maxAmmoCapacity;
    public int maxMagazineCapacity;
    public float hitForce;
    public float reloadTime;
    public float range;
    public float fireRate;
    public HitDetector hitDetector;
    public SoundSource soundProfile;
    public FireMode fireMode;
    public int burstCount;
    public float burstCycleCooldown = 0.3f;
    public WeaponVisualConfig weaponVisualConfig;
    public LayerMask hitMask;
    public bool usesSpread;
    public Vector3 bulletSpread;
}

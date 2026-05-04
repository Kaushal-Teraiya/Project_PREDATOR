using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/CameraRecoilProfile")]
public class RecoilConfig : ScriptableObject
{
    public float verticleRecoil;
    public float horizontalRecoil;
    public float weaponKickbackDistance;
    public float weaponKickbackRecoverySpeed;
    public float recoilRecoverySpeed;
    public Transform recoilPivot;
}

using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/HitDetectionType/Raycast")]
public class RaycastHitDetectionType : HitDetector
{
    public override bool DetectHit(Transform fireOrigin, Vector3 direction, WeaponFireConfig weaponConfig, out HitResult hitResult)
    {
        hitResult = default;
        RaycastHit hitInfo;
        Vector3 spreadDirection = direction;

        if (Physics.Raycast(fireOrigin.position, spreadDirection, out hitInfo, weaponConfig.range, weaponConfig.hitMask))
        {
            hitResult.success = true;
            hitResult.hitObject = hitInfo.collider.gameObject;
            hitResult.hitPosition = hitInfo.point;
            hitResult.hitDirection = spreadDirection;
            hitResult.hitForce = weaponConfig.hitForce;
            hitResult.hitNormal = hitInfo.normal;
            return true;
        }

        hitResult.success = false;
        return false;
    }
}

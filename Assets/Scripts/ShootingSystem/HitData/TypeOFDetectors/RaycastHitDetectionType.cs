using UnityEngine;

[CreateAssetMenu(menuName = "WeaponConfig/HitDetectionType/Raycast")]
public class RaycastHitDetectionType : HitDetector
{
    public override bool DetectHit(Transform fireOrigin, Vector3 direction, WeaponFireConfig weaponConfig, out HitResult hitResult)
    {
        hitResult = default;
        Vector3 spreadDirection = direction;
        RaycastHit[] hits = Physics.RaycastAll(fireOrigin.position, spreadDirection, weaponConfig.range, weaponConfig.hitMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hitInfo in hits)
        {
            if (hitInfo.collider.GetComponent<ProximitySensor>() != null)
            {
                continue;
            }
            Debug.Log(hitInfo.collider.name);
            Debug.Log(hitInfo.collider.transform.root.name);
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

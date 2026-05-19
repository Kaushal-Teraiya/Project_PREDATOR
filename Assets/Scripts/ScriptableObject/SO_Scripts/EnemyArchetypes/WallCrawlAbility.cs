using UnityEngine;

[CreateAssetMenu(fileName = "WallCrawlAbility" , menuName = "Enemy/Abilities/WallCrawl")]
public class WallCrawlAbility : EnemyAbility
{
    [Header("Detection")]
    public float rayDistance = 2f;
    public float headHeight = 1.5f;
    public float maxTiltAngle = 135f;
    public float minTiltAngle = 45f;
    public LayerMask WallMask;

    [Header("Movement")]
    // public float moveSpeed = 2f;
    public float turnSpeed = 5f;
    public float attachDistance = 0.8f;

    [Header("Timing")]
    public float tweakTiming = 0.12f;

    [Header("Mount")]
    public float rotationSpeed = 5f;
    public float positionSpeed = 5f;
    public float wallOffset = 0.3f;

    [Header("CrawlData")]
    public float crawlSpeed = 3f;
    public float roamDistance = 3f;


}

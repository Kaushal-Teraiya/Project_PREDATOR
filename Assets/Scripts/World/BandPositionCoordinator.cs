using UnityEngine;
public class BandPositionCoordinator : MonoBehaviour
{
    public static BandPositionCoordinator Instance;

    [SerializeField] private int closeBandMax = 2;
    [SerializeField] private int midBandMax = 3;
    [SerializeField] private int farBandMax = 4;

    private int closeBandCount;
    private int midBandCount;
    private int farBandCount;

    private void Awake()
    {
        Instance = this;
    }

    public bool TryReserveBand(EnemyBrain.CombatBand band)
    {
        switch (band)
        {
            case EnemyBrain.CombatBand.Close:
                if (closeBandCount >= closeBandMax) return false;
                closeBandCount++;
                return true;

            case EnemyBrain.CombatBand.Mid:
                if (midBandCount >= midBandMax) return false;
                midBandCount++;
                return true;

            case EnemyBrain.CombatBand.Far:
                if (farBandCount >= farBandMax) return false;
                farBandCount++;
                return true;
        }

        return false;
    }

    public void ReleaseBand(EnemyBrain.CombatBand band)
    {
        switch (band)
        {
            case EnemyBrain.CombatBand.Close:
                closeBandCount = Mathf.Max(0, closeBandCount - 1);
                break;

            case EnemyBrain.CombatBand.Mid:
                midBandCount = Mathf.Max(0, midBandCount - 1);
                break;

            case EnemyBrain.CombatBand.Far:
                farBandCount = Mathf.Max(0, farBandCount - 1);
                break;
        }
    }
}
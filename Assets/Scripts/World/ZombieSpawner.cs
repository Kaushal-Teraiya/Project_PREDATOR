using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject walkerPrefab;
    [SerializeField] private GameObject crawlerPrefab;

    [Header("Spawn Points (Children of Player)")]
    [SerializeField] private Transform walkerSpawnPoint;
    [SerializeField] private Transform crawlerSpawnPoint;

    private GameObject currentWalker;
    private GameObject currentCrawler;

    public void SpawnZombies()
    {
        // Don't spawn duplicates
        if (currentWalker != null || currentCrawler != null)
            return;

        currentWalker = Instantiate(
            walkerPrefab,
            walkerSpawnPoint.position,
            walkerSpawnPoint.rotation);

        currentCrawler = Instantiate(
            crawlerPrefab,
            crawlerSpawnPoint.position,
            crawlerSpawnPoint.rotation);
    }

    private void Update()
    {
        // Clear references when zombies die
        if (currentWalker == null)
            currentWalker = null;

        if (currentCrawler == null)
            currentCrawler = null;
    }
}
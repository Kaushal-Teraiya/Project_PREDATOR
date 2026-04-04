using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public static class TrailController
{
    private static ObjectPool<BulletTrails> trailPool;
    private static TrailConfig cachedTrailConfig;
    public static void SpawnTrail(Transform _startPoint, Vector3 endPoint, TrailConfig trailConfig, MonoBehaviour coroutineRunner)
    {
        if (trailConfig == null || _startPoint == null || trailConfig.trailPrefab == null)
        {
            return;
        }

        if (trailPool == null || cachedTrailConfig != trailConfig)
        {
            InitializePool(trailConfig);
        }

        Vector3 startPoint = _startPoint.position;
        BulletTrails trailInstance = trailPool.Get();
        trailInstance.transform.position = startPoint;
        trailInstance.transform.forward = (endPoint - startPoint).normalized;
        trailInstance.Initialize(trailConfig);
        coroutineRunner.StartCoroutine(PlayTrail(trailInstance, startPoint, endPoint, trailConfig));
    }

    private static void InitializePool(TrailConfig trailConfig)
    {
        cachedTrailConfig = trailConfig;
        trailPool = new ObjectPool<BulletTrails>(
            () =>
            {
                GameObject _gameObject = Object.Instantiate(trailConfig.trailPrefab);
                return _gameObject.GetComponent<BulletTrails>();
            },

            trail =>
            {
                trail.gameObject.SetActive(true);
            },
            trail =>
            {
                trail.Clear();
                trail.gameObject.SetActive(false);
            },
            trail =>
            {
                
            },
            false,
            10,
            50
        );
    }

    private static IEnumerator PlayTrail(BulletTrails trails, Vector3 startPoint, Vector3 endPoint, TrailConfig trailConfig)
    {
        float distance = Vector3.Distance(startPoint, endPoint);
        if (distance <= 0.001f)
        {
            trailPool.Release(trails);
            yield break;
        }

        trails.transform.forward = (endPoint - startPoint).normalized;

        float duration = distance / trailConfig.trailSpeed;
        float inverseDuration = 1f / duration;
        float time = 0f;
        while (time < duration)
        {
            if (trails == null)
            {
                yield break;
            }
            float progress = time * inverseDuration;
            trails.transform.position = Vector3.Lerp(startPoint, endPoint, progress);
            time += Time.deltaTime;
            yield return null;
        }

        trails.transform.position = endPoint;
        yield return new WaitForSeconds(trailConfig.duration);
        if (trails != null)
        {
            trailPool.Release(trails);
        }

    }

}

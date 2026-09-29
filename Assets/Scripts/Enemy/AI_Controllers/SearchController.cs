using System;
using System.Collections.Generic;
using UnityEngine;

public class SearchController
{
    private readonly EnemyBrain brain;

    private readonly List<SearchPoint> availableSearchPoints = new List<SearchPoint>();
    private readonly List<SearchPoint> selectedSearchPoints = new List<SearchPoint>();

    private int currentSearchIndex;
    private SearchPoint lastReleasedPoint;
    private float lastReleaseTime;

    public int CurrentSearchIndex => currentSearchIndex;

    public bool IsSearchComplete => currentSearchIndex >= selectedSearchPoints.Count;

    public SearchController(EnemyBrain brain)
    {
        this.brain = brain;
    }

    public void InitializeSearch()
    {
        CollectNearbySearchPoints();
        ShuffleSearchPoints();

        var pickNsearchPoints = availableSearchPoints.Count * brain.SearchPointFactorPercent;
        int roundUp = (int)Mathf.Ceil((float)pickNsearchPoints);
        int finalNsearchPoints = Mathf.Clamp(roundUp, 2, 8);
        finalNsearchPoints = (int)MathF.Min(finalNsearchPoints, availableSearchPoints.Count);

        for (int i = 0; i < finalNsearchPoints; i++)
        {
            selectedSearchPoints.Add(availableSearchPoints[i]);
        }

        foreach (var searchPoint in selectedSearchPoints)
        {
            searchPoint.GenerateSlots();
        }

        if (brain.PostChase)
        {
            for (int i = selectedSearchPoints.Count - 1; i >= 0; i--)
            {
                Vector3 toPoint = selectedSearchPoints[i].transform.position - brain.CurrentInvestigationCenter;
                toPoint.y = 0f;

                if (Vector3.Dot(brain.InvestigationForward.normalized, toPoint.normalized) <= 0f)
                {
                    selectedSearchPoints.RemoveAt(i);
                }
            }
        }

        currentSearchIndex = 0;
    }

    private void CollectNearbySearchPoints()
    {
        selectedSearchPoints.Clear();
        availableSearchPoints.Clear();

        Collider[] points = Physics.OverlapSphere(
            brain.CurrentInvestigationCenter,
            brain.CurrentInvestigationRadius,
            brain.SearchPointLayer);

        foreach (var point in points)
        {
            if (point != null)
            {
                var searchPoint = point.gameObject.GetComponent<SearchPoint>();

                if (searchPoint == null)
                {
                    Debug.Log("null search point");
                    continue;
                }

                availableSearchPoints.Add(searchPoint);
            }
        }
    }

    public SearchPoint GetCurrentSearchPoint()
    {
        if (currentSearchIndex >= 0 && currentSearchIndex < selectedSearchPoints.Count)
            return selectedSearchPoints[currentSearchIndex];

        return null;
    }

    public void IncrementSearchIndex()
    {
        currentSearchIndex++;
    }

    private void ShuffleSearchPoints()
    {
        for (int currentIndex = availableSearchPoints.Count - 1; currentIndex > 0; currentIndex--)
        {
            int randomIndex = UnityEngine.Random.Range(0, currentIndex + 1);
            var temp = availableSearchPoints[currentIndex];
            availableSearchPoints[currentIndex] = availableSearchPoints[randomIndex];
            availableSearchPoints[randomIndex] = temp;
        }
    }

    public void NotifySearchPointReleased(SearchPoint point)
    {
        lastReleasedPoint = point;
        lastReleaseTime = Time.time;
    }

    public bool CanClaim(SearchPoint point)
    {
        if (point == lastReleasedPoint && Time.time - lastReleaseTime < 0.3f)
            return false;

        return true;
    }
}
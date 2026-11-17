using System;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Turns a SplineContainer into an arc-length lookup table, so balls can be
/// positioned by "distance travelled along the path" (what a Zuma-style chain needs).
/// </summary>
[RequireComponent(typeof(SplineContainer))]
[DefaultExecutionOrder(-50)]
public class PathSampler : MonoBehaviour
{
    [SerializeField, Min(32)] int samples = 1000;

    SplineContainer container;
    Vector3[] points;
    float[] cumulative;

    public float Length { get; private set; }
    public bool IsReady => points != null && Length > 0f;

    void Awake()
    {
        Rebuild();
    }

public void Rebuild()
    {
        container = GetComponent<SplineContainer>();
        points = new Vector3[samples + 1];
        cumulative = new float[samples + 1];

        for (int sampleIndex = 0; sampleIndex <= samples; sampleIndex++)
        {
            var sampledPoint = container.EvaluatePosition(sampleIndex / (float)samples);
            points[sampleIndex] = new Vector3(sampledPoint.x, sampledPoint.y, sampledPoint.z);
            cumulative[sampleIndex] = sampleIndex == 0
                ? 0f
                : cumulative[sampleIndex - 1] + Vector3.Distance(points[sampleIndex - 1], points[sampleIndex]);
        }

        Length = cumulative[samples];
    }

    /// <summary>World position at the given distance (in world units) from the path start.</summary>
public Vector3 GetPosition(float distance)
    {
        if (!IsReady) return transform.position;

        distance = Mathf.Clamp(distance, 0f, Length);
        int foundIndex = Array.BinarySearch(cumulative, distance);
        if (foundIndex >= 0) return points[Mathf.Min(foundIndex, samples)];

        int upperIndex = Mathf.Clamp(~foundIndex, 1, samples);
        float segmentLength = cumulative[upperIndex] - cumulative[upperIndex - 1];
        float segmentProgress = segmentLength > 1e-5f ? (distance - cumulative[upperIndex - 1]) / segmentLength : 0f;
        return Vector3.Lerp(points[upperIndex - 1], points[upperIndex], segmentProgress);
    }

/// <summary>Distance along the path of the sample closest to a world position.</summary>
    public float GetClosestDistance(Vector3 worldPosition)
    {
        if (!IsReady) return 0f;

        int closestIndex = 0;
        float closestSquaredDistance = float.MaxValue;
        for (int sampleIndex = 0; sampleIndex < points.Length; sampleIndex++)
        {
            float squaredDistance = ((Vector2)(points[sampleIndex] - worldPosition)).sqrMagnitude;
            if (squaredDistance < closestSquaredDistance)
            {
                closestSquaredDistance = squaredDistance;
                closestIndex = sampleIndex;
            }
        }

        return cumulative[closestIndex];
    }


void OnDrawGizmos()
    {
        var splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Spline == null || splineContainer.Spline.Count < 2) return;

        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.9f);
        const int steps = 200;
        var firstPoint = splineContainer.EvaluatePosition(0f);
        Vector3 previousPoint = new Vector3(firstPoint.x, firstPoint.y, firstPoint.z);
        for (int stepIndex = 1; stepIndex <= steps; stepIndex++)
        {
            var sampledPoint = splineContainer.EvaluatePosition(stepIndex / (float)steps);
            var currentPoint = new Vector3(sampledPoint.x, sampledPoint.y, sampledPoint.z);
            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}

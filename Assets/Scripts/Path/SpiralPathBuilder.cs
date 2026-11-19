using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Fills an empty SplineContainer with a spiral that goes from the outside inwards.
/// Edit the knots by hand afterwards in the Scene view to design real levels.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(SplineContainer))]
public class SpiralPathBuilder : MonoBehaviour
{
    [SerializeField, Min(1)] int turns = 3;
    [SerializeField, Min(4)] int knotsPerTurn = 8;
    [SerializeField] float startRadius = 4.5f;
    [SerializeField] float endRadius = 1.2f;

    void OnEnable()
    {
        var container = GetComponent<SplineContainer>();
        if (container.Spline == null || container.Spline.Count < 2)
            Build();
    }

[ContextMenu("Rebuild Spiral")]
    public void Build()
    {
        var container = GetComponent<SplineContainer>();
        var spline = container.Spline;
        spline.Clear();

        int totalKnots = turns * knotsPerTurn;
        for (int knotIndex = 0; knotIndex <= totalKnots; knotIndex++)
        {
            float progress = knotIndex / (float)totalKnots;
            float angle = progress * turns * Mathf.PI * 2f;
            float radius = Mathf.Lerp(startRadius, endRadius, progress);
            var knotPosition = new float3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            spline.Add(new BezierKnot(knotPosition), TangentMode.AutoSmooth);
        }
    }

    /// <summary>Changes the spiral shape (used when a level loads) and rebuilds the spline.</summary>
    public void Configure(int newTurns, float newStartRadius, float newEndRadius)
    {
        turns = Mathf.Max(1, newTurns);
        startRadius = newStartRadius;
        endRadius = newEndRadius;
        Build();
    }

}

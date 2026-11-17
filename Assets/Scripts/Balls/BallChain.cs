using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The chain of balls rolling along the path. Ball 0 is the head (closest to the end);
/// every following ball trails one diameter behind the previous one.
/// Week 1: spawn + constant-speed movement. Insertion and matching come next.
/// </summary>
public class BallChain : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] PathSampler path;

    [Header("Chain")]
    [SerializeField, Min(1)] int ballCount = 40;
    [SerializeField, Min(0.1f)] float ballDiameter = 0.6f;
    [SerializeField, Min(0f)] float speed = 1.2f;

    [Header("Ball types (placeholder colours until real art arrives)")]
    [SerializeField]
    Color[] colors =
    {
        new Color(0.85f, 0.27f, 0.22f), // red
        new Color(0.30f, 0.62f, 0.90f), // blue
        new Color(0.95f, 0.80f, 0.25f), // yellow
        new Color(0.40f, 0.75f, 0.38f), // green
        new Color(0.70f, 0.45f, 0.85f), // purple
    };

    readonly List<Ball> balls = new List<Ball>();
    float headDistance;
    bool reachedEnd;

    public event Action ChainReachedEnd;

    public float BallDiameter => ballDiameter;
    public IReadOnlyList<Ball> Balls => balls;

    void Start()
    {
        if (path == null) path = FindAnyObjectByType<PathSampler>();
        if (path == null)
        {
            Debug.LogError("BallChain: no PathSampler found in the scene.", this);
            enabled = false;
            return;
        }

        Spawn();
    }

void Spawn()
    {
        for (int ballIndex = 0; ballIndex < ballCount; ballIndex++)
        {
            int typeIndex = PickType();
            var ballObject = new GameObject($"Ball_{ballIndex}");
            ballObject.transform.SetParent(transform, false);

            var ball = ballObject.AddComponent<Ball>();
            ball.Init(typeIndex, colors[typeIndex], ballDiameter);

            ballObject.SetActive(false); // hidden until it enters the path
            balls.Add(ball);
        }
    }

    // Random type, but never three of the same colour in a row at spawn.
int PickType()
    {
        int ballIndex = balls.Count;
        int typeIndex;
        int attempts = 0;
        do
        {
            typeIndex = UnityEngine.Random.Range(0, colors.Length);
            attempts++;
        }
        while (ballIndex >= 2 && balls[ballIndex - 1].Type == typeIndex && balls[ballIndex - 2].Type == typeIndex && attempts < 20);
        return typeIndex;
    }

void Update()
    {
        if (reachedEnd) return;

        headDistance += speed * Time.deltaTime;

        for (int ballIndex = 0; ballIndex < balls.Count; ballIndex++)
        {
            var ball = balls[ballIndex];
            float travelledDistance = headDistance - ballIndex * ballDiameter;
            ball.Distance = travelledDistance;

            bool isOnPath = travelledDistance >= 0f;
            if (ball.gameObject.activeSelf != isOnPath)
                ball.gameObject.SetActive(isOnPath);

            if (isOnPath)
                ball.transform.position = path.GetPosition(travelledDistance);
        }

        if (headDistance >= path.Length)
        {
            reachedEnd = true;
            Debug.Log("BallChain: the head reached the end of the path (game over in the full game).");
            ChainReachedEnd?.Invoke();
        }
    }
}

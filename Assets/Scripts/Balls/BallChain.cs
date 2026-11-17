using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The chain of balls rolling along the path. Ball 0 is the head (closest to the end);
/// every following ball trails one diameter behind the previous one.
/// Week 2: balls can be inserted into the chain; matching and removal come next.
/// </summary>
public class BallChain : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] PathSampler path;

    [Header("Chain")]
    [SerializeField, Min(1)] int ballCount = 40;
    [SerializeField, Min(0.1f)] float ballDiameter = 0.6f;
    [SerializeField, Min(0f)] float speed = 1.2f;
    [SerializeField, Min(0f)] float catchUpSpeed = 10f; // how fast balls slide into place after an insertion

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
    int nextBallId;

    public event Action ChainReachedEnd;

    public float BallDiameter => ballDiameter;
    public int TypeCount => colors.Length;
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
            var ball = CreateBall(typeIndex, headDistance - ballIndex * ballDiameter);
            ball.gameObject.SetActive(false); // hidden until it enters the path
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

    Ball CreateBall(int typeIndex, float startDistance)
    {
        var ballObject = new GameObject($"Ball_{nextBallId}");
        nextBallId++;
        ballObject.transform.SetParent(transform, false);

        var ball = ballObject.AddComponent<Ball>();
        ball.Init(typeIndex, colors[typeIndex], ballDiameter);
        ball.Distance = startDistance;
        return ball;
    }

    void Update()
    {
        if (reachedEnd) return;

        headDistance += speed * Time.deltaTime;
        float maxStep = catchUpSpeed * Time.deltaTime;

        for (int ballIndex = 0; ballIndex < balls.Count; ballIndex++)
        {
            var ball = balls[ballIndex];
            float targetDistance = headDistance - ballIndex * ballDiameter;
            ball.Distance = Mathf.MoveTowards(ball.Distance, targetDistance, maxStep);

            bool isOnPath = ball.Distance >= 0f;
            if (ball.gameObject.activeSelf != isOnPath)
                ball.gameObject.SetActive(isOnPath);

            if (isOnPath)
                ball.transform.position = path.GetPosition(ball.Distance);
        }

        if (headDistance >= path.Length)
        {
            reachedEnd = true;
            Debug.Log("BallChain: the head reached the end of the path (game over in the full game).");
            ChainReachedEnd?.Invoke();
        }
    }

    public Color GetColor(int typeIndex)
    {
        return colors[typeIndex];
    }

    /// <summary>Finds the visible ball nearest to a world position, within hitRadius.</summary>
    public bool TryFindHit(Vector3 worldPosition, float hitRadius, out int hitBallIndex)
    {
        hitBallIndex = -1;
        float closestSquaredDistance = hitRadius * hitRadius;

        for (int ballIndex = 0; ballIndex < balls.Count; ballIndex++)
        {
            var ball = balls[ballIndex];
            if (!ball.gameObject.activeSelf) continue;

            float squaredDistance = ((Vector2)(ball.transform.position - worldPosition)).sqrMagnitude;
            if (squaredDistance < closestSquaredDistance)
            {
                closestSquaredDistance = squaredDistance;
                hitBallIndex = ballIndex;
            }
        }

        return hitBallIndex >= 0;
    }

    /// <summary>
    /// Inserts a new ball next to the ball that was hit. Everything ahead of the new ball
    /// is pushed forward by one diameter (the head shifts); everything behind keeps its place.
    /// </summary>
    public void InsertBall(int typeIndex, Vector3 hitPosition, int hitBallIndex)
    {
        var hitBall = balls[hitBallIndex];
        float hitPathDistance = path.GetClosestDistance(hitPosition);

        // Landed ahead of the hit ball (towards the head) -> take its slot; otherwise go behind it.
        int insertIndex = hitPathDistance > hitBall.Distance ? hitBallIndex : hitBallIndex + 1;

        headDistance += ballDiameter;

        var newBall = CreateBall(typeIndex, hitPathDistance);
        balls.Insert(insertIndex, newBall);
    }

    /// <summary>Picks a type that is still present in the chain, so the player never gets an impossible colour.</summary>
    public int PickShooterType()
    {
        var presentTypes = new List<int>();
        foreach (var ball in balls)
        {
            if (!presentTypes.Contains(ball.Type)) presentTypes.Add(ball.Type);
        }

        if (presentTypes.Count == 0) return UnityEngine.Random.Range(0, colors.Length);
        return presentTypes[UnityEngine.Random.Range(0, presentTypes.Count)];
    }
}

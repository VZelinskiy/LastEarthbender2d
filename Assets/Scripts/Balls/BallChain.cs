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
    int ballCount; // set by BeginLevel
    [SerializeField, Min(0.1f)] float ballDiameter = 0.6f;
    float speed; // set by BeginLevel
    [SerializeField, Min(0f)] float catchUpSpeed = 10f; // how fast balls slide into place after an insertion
    [SerializeField, Min(0f)] float rollbackSpeed = 3f; // how fast the front part rolls back while a gap closes

    [Header("Bonuses")]
    [SerializeField, Min(0f)] float slowDuration = 6f;
    [SerializeField, Range(0.05f, 1f)] float slowFactor = 0.4f; // chain speed multiplier while slowed
    [SerializeField, Min(0f)] float reverseDuration = 3f;
    [SerializeField, Min(0f)] float reverseSpeed = 2f; // kept below rollbackSpeed so gaps still close
    [SerializeField, Min(0)] int bombRadiusInBalls = 2;

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
    bool isFinished;
    int nextBallId;
    int activeTypeCount; // how many of the colours this level uses
    float bonusChance;
    float slowTimer;
    float reverseTimer;

    const int MinimumGroupSize = 3;
    const float SettledTolerance = 0.02f;
    const float GapEpsilon = 0.001f;
    const int MinimumBallsBeforeBonus = 4;
    const float PopDuration = 0.12f;

    // A spot where a match should be looked for once the balls there have stopped sliding.
    struct PendingCheck
    {
        public Ball Ball;
        public bool IsSeam;      // true: a gap just closed here; false: a freshly inserted ball
        public int ComboLevel;
    }

    readonly List<PendingCheck> pendingChecks = new List<PendingCheck>();

    public event Action ChainReachedEnd;
    public event Action ChainCleared;
    public event Action<int, int> BallsCleared; // (balls removed, combo level)
    public event Action<BallKind> BonusTriggered;

    public float BallDiameter => ballDiameter;
    public int TypeCount => activeTypeCount;
    public int BallsRemaining => balls.Count;
    public bool IsFinished => isFinished;
    public float SlowRemaining => slowTimer;
    public float ReverseRemaining => reverseTimer;
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

        if (activeTypeCount == 0) activeTypeCount = colors.Length;
    }

    /// <summary>Clears the chain and spawns a fresh one for a level.</summary>
    public void BeginLevel(int newBallCount, int newTypeCount, float newChainSpeed, float newBonusChance)
    {
        if (path == null) path = FindAnyObjectByType<PathSampler>();

        ClearAll();
        ballCount = newBallCount;
        activeTypeCount = Mathf.Clamp(newTypeCount, 1, colors.Length);
        speed = newChainSpeed;
        bonusChance = newBonusChance;
        slowTimer = 0f;
        reverseTimer = 0f;
        headDistance = 0f;
        nextBallId = 0;
        isFinished = false;

        Spawn();
    }

    void ClearAll()
    {
        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
            Destroy(transform.GetChild(childIndex).gameObject);

        balls.Clear();
        pendingChecks.Clear();
    }


    void Spawn()
    {
        for (int ballIndex = 0; ballIndex < ballCount; ballIndex++)
        {
            int typeIndex = PickType();
            var ball = CreateBall(typeIndex, headDistance - ballIndex * ballDiameter);

            if (ballIndex >= MinimumBallsBeforeBonus && UnityEngine.Random.value < bonusChance)
                ball.SetKind(PickBonusKind());

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
            typeIndex = UnityEngine.Random.Range(0, activeTypeCount);
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
        if (isFinished) return;

        slowTimer = Mathf.Max(0f, slowTimer - Time.deltaTime);
        reverseTimer = Mathf.Max(0f, reverseTimer - Time.deltaTime);

        AdvanceChain(Time.deltaTime);

        float maxStep = catchUpSpeed * Time.deltaTime;
        float accumulatedGap = 0f;

        for (int ballIndex = 0; ballIndex < balls.Count; ballIndex++)
        {
            var ball = balls[ballIndex];
            accumulatedGap += ball.GapAhead;

            float targetDistance = headDistance - ballIndex * ballDiameter - accumulatedGap;
            ball.Distance = Mathf.MoveTowards(ball.Distance, targetDistance, maxStep);

            bool isOnPath = IsOnPath(ball);
            if (ball.gameObject.activeSelf != isOnPath)
                ball.gameObject.SetActive(isOnPath);

            if (isOnPath)
                ball.transform.position = path.GetPosition(ball.Distance);
        }

        ProcessPendingChecks();

        if (!isFinished && headDistance >= path.Length)
        {
            isFinished = true;
            Debug.Log("BallChain: the head reached the end of the path (game over in the full game).");
            ChainReachedEnd?.Invoke();
        }
    }

    /// <summary>
    /// Moves the chain. Gaps split it into segments: the last segment is pushed forward at the
    /// normal speed, and everything ahead of a gap rolls back until that gap closes.
    /// </summary>
    void AdvanceChain(float deltaTime)
    {
        float tailVelocity = GetTailVelocity();

        int tailSegmentStart = FindTailSegmentStart();
        if (tailSegmentStart == 0)
        {
            headDistance += tailVelocity * deltaTime;
            return;
        }

        headDistance -= rollbackSpeed * deltaTime;

        // The tail segment moves at tailVelocity and the part ahead rolls back, so the gap closes at the sum of both.
        var tailSegmentFirstBall = balls[tailSegmentStart];
        tailSegmentFirstBall.GapAhead -= (rollbackSpeed + tailVelocity) * deltaTime;

        if (tailSegmentFirstBall.GapAhead <= GapEpsilon)
        {
            tailSegmentFirstBall.GapAhead = 0f;
            pendingChecks.Add(new PendingCheck { Ball = tailSegmentFirstBall, IsSeam = true, ComboLevel = tailSegmentFirstBall.SeamComboLevel });
        }
    }

    // Index of the first ball of the last segment (0 when the chain has no gaps).
    int FindTailSegmentStart()
    {
        for (int ballIndex = balls.Count - 1; ballIndex > 0; ballIndex--)
        {
            if (balls[ballIndex].GapAhead > GapEpsilon) return ballIndex;
        }

        return 0;
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
        float hitPathDistance = path.GetClosestDistance(hitPosition, hitBall.Distance, ballDiameter * 1.5f);

        // Landed ahead of the hit ball (towards the head) -> take its slot; otherwise go behind it.
        int insertIndex = hitPathDistance > hitBall.Distance ? hitBallIndex : hitBallIndex + 1;

        // Make room for the new ball: use up any gap at that spot first, push everything ahead for the rest.
        float gapAtSlot = insertIndex < balls.Count ? balls[insertIndex].GapAhead : 0f;
        float absorbedGap = Mathf.Min(gapAtSlot, ballDiameter);
        headDistance += ballDiameter - absorbedGap;

        if (insertIndex < balls.Count)
        {
            float remainingGap = gapAtSlot - absorbedGap;
            balls[insertIndex].GapAhead = remainingGap > GapEpsilon ? remainingGap : 0f;
        }

        var newBall = CreateBall(typeIndex, hitPathDistance);
        balls.Insert(insertIndex, newBall);

        // Look for a match once the new ball has slid into its slot.
        pendingChecks.Add(new PendingCheck { Ball = newBall, IsSeam = false, ComboLevel = 1 });
    }

    /// <summary>Picks a type that is still present in the chain, so the player never gets an impossible colour.</summary>
    public int PickShooterType()
    {
        var presentTypes = new List<int>();
        CollectTypes(presentTypes, true);
        if (presentTypes.Count == 0) CollectTypes(presentTypes, false);

        if (presentTypes.Count == 0) return UnityEngine.Random.Range(0, activeTypeCount);
        return presentTypes[UnityEngine.Random.Range(0, presentTypes.Count)];
    }

    void CollectTypes(List<int> typeList, bool onPathOnly)
    {
        foreach (var ball in balls)
        {
            if (onPathOnly && !IsOnPath(ball)) continue;
            if (!typeList.Contains(ball.Type)) typeList.Add(ball.Type);
        }
    }

    float GetTargetDistance(int ballIndex)
    {
        float accumulatedGap = 0f;
        for (int gapIndex = 1; gapIndex <= ballIndex; gapIndex++)
            accumulatedGap += balls[gapIndex].GapAhead;

        return headDistance - ballIndex * ballDiameter - accumulatedGap;
    }

    static bool IsOnPath(Ball ball)
    {
        return ball.Distance >= 0f;
    }

    int FindBallIndex(Ball searchedBall)
    {
        for (int ballIndex = 0; ballIndex < balls.Count; ballIndex++)
        {
            if (balls[ballIndex] == searchedBall) return ballIndex;
        }

        return -1;
    }

    /// <summary>Finds the run of same-type balls (already on the path) around centerIndex.</summary>
    bool TryFindGroup(int centerIndex, out int firstIndex, out int lastIndex)
    {
        int typeIndex = balls[centerIndex].Type;
        firstIndex = centerIndex;
        lastIndex = centerIndex;

        // Balls only count as a group while they actually touch (no gap between them).
        while (firstIndex > 0
               && balls[firstIndex].GapAhead <= GapEpsilon
               && IsOnPath(balls[firstIndex - 1])
               && balls[firstIndex - 1].Type == typeIndex)
            firstIndex--;

        while (lastIndex < balls.Count - 1
               && balls[lastIndex + 1].GapAhead <= GapEpsilon
               && IsOnPath(balls[lastIndex + 1])
               && balls[lastIndex + 1].Type == typeIndex)
            lastIndex++;

        return lastIndex - firstIndex + 1 >= MinimumGroupSize;
    }

    void RemoveRange(int firstIndex, int lastIndex, int seamComboLevel)
    {
        int removedCount = lastIndex - firstIndex + 1;
        bool hasBallBehind = lastIndex + 1 < balls.Count;

        float newHeadDistance = headDistance;
        float newGap = 0f;
        if (hasBallBehind)
        {
            // The ball behind the group ends up this far from the ball ahead of the group (minus one diameter).
            newGap = removedCount * ballDiameter;
            for (int ballIndex = firstIndex; ballIndex <= lastIndex + 1; ballIndex++)
                newGap += balls[ballIndex].GapAhead;

            // Nothing is left ahead of the gap, so the ball behind becomes the head and must not jump forward.
            if (firstIndex == 0)
                newHeadDistance = GetTargetDistance(lastIndex + 1);
        }

        for (int ballIndex = firstIndex; ballIndex <= lastIndex; ballIndex++)
            balls[ballIndex].Pop(PopDuration);

        balls.RemoveRange(firstIndex, removedCount);
        headDistance = newHeadDistance;

        if (!hasBallBehind) return;

        var ballBehind = balls[firstIndex];
        if (firstIndex == 0)
        {
            ballBehind.GapAhead = 0f;
            ballBehind.SeamComboLevel = 0;
        }
        else
        {
            ballBehind.GapAhead = newGap;
            ballBehind.SeamComboLevel = seamComboLevel;
        }
    }

    void ProcessPendingChecks()
    {
        int checkIndex = 0;
        while (checkIndex < pendingChecks.Count)
        {
            var check = pendingChecks[checkIndex];
            int ballIndex = FindBallIndex(check.Ball);

            if (ballIndex < 0)
            {
                // The ball was removed by an earlier match.
                pendingChecks.RemoveAt(checkIndex);
                continue;
            }

            float distanceToSlot = Mathf.Abs(balls[ballIndex].Distance - GetTargetDistance(ballIndex));
            if (distanceToSlot >= SettledTolerance)
            {
                checkIndex++;
                continue;
            }

            pendingChecks.RemoveAt(checkIndex);
            ResolveCheck(check, ballIndex);
        }
    }

    void ResolveCheck(PendingCheck check, int ballIndex)
    {
        if (!IsOnPath(balls[ballIndex])) return;

        if (check.IsSeam)
        {
            // The gap has just closed: only a match across the seam continues the combo.
            if (ballIndex == 0 || balls[ballIndex - 1].Type != balls[ballIndex].Type) return;
        }

        int firstIndex;
        int lastIndex;
        if (!TryFindGroup(ballIndex, out firstIndex, out lastIndex)) return;

        ClearRange(firstIndex, lastIndex, check.ComboLevel);
    }

    /// <summary>
    /// Removes a run of balls, then triggers the bonuses they carried. A bomb among them
    /// widens the cleared run by bombRadiusInBalls on each side.
    /// </summary>
    void ClearRange(int firstIndex, int lastIndex, int comboLevel)
    {
        bool hasBomb = false;
        for (int ballIndex = firstIndex; ballIndex <= lastIndex; ballIndex++)
        {
            if (balls[ballIndex].Kind == BallKind.Bomb) hasBomb = true;
        }

        if (hasBomb)
        {
            firstIndex = Mathf.Max(0, firstIndex - bombRadiusInBalls);
            lastIndex = Mathf.Min(balls.Count - 1, lastIndex + bombRadiusInBalls);

            // Balls that have not entered the path yet are not part of the explosion.
            while (lastIndex > firstIndex && !IsOnPath(balls[lastIndex])) lastIndex--;
        }

        var triggeredKinds = new List<BallKind>();
        for (int ballIndex = firstIndex; ballIndex <= lastIndex; ballIndex++)
        {
            BallKind kind = balls[ballIndex].Kind;
            if (kind != BallKind.Normal && !triggeredKinds.Contains(kind)) triggeredKinds.Add(kind);
        }

        int removedCount = lastIndex - firstIndex + 1;
        RemoveRange(firstIndex, lastIndex, comboLevel + 1);
        Debug.Log($"BallChain: cleared {removedCount} balls (combo x{comboLevel})");
        BallsCleared?.Invoke(removedCount, comboLevel);

        foreach (BallKind kind in triggeredKinds)
            ApplyBonus(kind);

        if (balls.Count == 0)
        {
            isFinished = true;
            ChainCleared?.Invoke();
        }
    }

    /// <summary>Earth Strike: clears the hit ball and its neighbours, whatever their colours.</summary>
    public void StrikeAround(int hitBallIndex, int radiusInBalls)
    {
        if (hitBallIndex < 0 || hitBallIndex >= balls.Count) return;

        int firstIndex = Mathf.Max(0, hitBallIndex - radiusInBalls);
        int lastIndex = Mathf.Min(balls.Count - 1, hitBallIndex + radiusInBalls);
        while (lastIndex > hitBallIndex && !IsOnPath(balls[lastIndex])) lastIndex--;

        ClearRange(firstIndex, lastIndex, 1);
    }

    void ApplyBonus(BallKind kind)
    {
        switch (kind)
        {
            case BallKind.Slow:
                slowTimer = slowDuration;
                break;
            case BallKind.Reverse:
                reverseTimer = reverseDuration;
                break;
        }

        Debug.Log($"BallChain: bonus {kind} triggered");
        BonusTriggered?.Invoke(kind);
    }

    // Speed of the last segment: normal, slowed, or negative while the chain is reversing.
    float GetTailVelocity()
    {
        if (reverseTimer > 0f) return -Mathf.Min(reverseSpeed, rollbackSpeed * 0.9f);
        return slowTimer > 0f ? speed * slowFactor : speed;
    }

    static BallKind PickBonusKind()
    {
        return (BallKind)UnityEngine.Random.Range(1, 5);
    }


}

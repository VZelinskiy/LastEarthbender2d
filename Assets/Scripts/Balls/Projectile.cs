using UnityEngine;

/// <summary>
/// A ball in flight. Moves in a straight line until it touches the chain, then gets
/// inserted into it; if it never hits anything it is removed once it leaves the playfield.
/// </summary>
public class Projectile : MonoBehaviour
{
    const float HitRadiusInDiameters = 0.85f;
    const float MaxDistanceFromOrigin = 14f;
    const float MaxStepLength = 0.2f; // smaller than a ball, so no frame can skip over one

    BallChain chain;
    int typeIndex;
    Vector2 direction;
    float speed;
    bool isStrike;
    int strikeRadiusInBalls;

    public void Launch(BallChain targetChain, int projectileType, Vector2 launchDirection, float launchSpeed)
    {
        chain = targetChain;
        typeIndex = projectileType;
        direction = launchDirection.normalized;
        speed = launchSpeed;

        var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = PlaceholderSprites.Circle;
        spriteRenderer.color = chain.GetColor(typeIndex);
        spriteRenderer.sortingOrder = 5;
        transform.localScale = Vector3.one * chain.BallDiameter;
    }

    /// <summary>Launches the heavy stone of the Earth Strike ability instead of an ordinary ball.</summary>
    public void LaunchStrike(BallChain targetChain, Vector2 launchDirection, float launchSpeed, int radiusInBalls)
    {
        chain = targetChain;
        direction = launchDirection.normalized;
        speed = launchSpeed;
        isStrike = true;
        strikeRadiusInBalls = radiusInBalls;

        var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = PlaceholderSprites.Circle;
        spriteRenderer.color = new Color(0.42f, 0.30f, 0.18f);
        spriteRenderer.sortingOrder = 6;
        transform.localScale = Vector3.one * chain.BallDiameter * 1.5f;
    }


    void Update()
    {
        // Move in small steps: a slow frame must not let a fast ball jump over the chain.
        float distanceLeft = speed * Time.deltaTime;
        while (distanceLeft > 0f)
        {
            float stepLength = Mathf.Min(distanceLeft, MaxStepLength);
            transform.position += (Vector3)(direction * stepLength);
            distanceLeft -= stepLength;

            if (chain.TryFindHit(transform.position, chain.BallDiameter * HitRadiusInDiameters, out int hitBallIndex))
            {
                if (isStrike) chain.StrikeAround(hitBallIndex, strikeRadiusInBalls);
                else chain.InsertBall(typeIndex, transform.position, hitBallIndex);

                Destroy(gameObject);
                return;
            }
        }

        if (transform.position.sqrMagnitude > MaxDistanceFromOrigin * MaxDistanceFromOrigin)
            Destroy(gameObject);
    }
}

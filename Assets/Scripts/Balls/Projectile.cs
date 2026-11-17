using UnityEngine;

/// <summary>
/// A ball in flight. Moves in a straight line until it touches the chain, then gets
/// inserted into it; if it never hits anything it is removed once it leaves the playfield.
/// </summary>
public class Projectile : MonoBehaviour
{
    const float HitRadiusInDiameters = 0.85f;
    const float MaxDistanceFromOrigin = 14f;

    BallChain chain;
    int typeIndex;
    Vector2 direction;
    float speed;

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

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (chain.TryFindHit(transform.position, chain.BallDiameter * HitRadiusInDiameters, out int hitBallIndex))
        {
            chain.InsertBall(typeIndex, transform.position, hitBallIndex);
            Destroy(gameObject);
            return;
        }

        if (transform.position.sqrMagnitude > MaxDistanceFromOrigin * MaxDistanceFromOrigin)
            Destroy(gameObject);
    }
}

/// <summary>Special power a ball carries. Normal balls just match by colour.</summary>
public enum BallKind
{
    Normal = 0,
    Slow = 1,     // slows the chain for a while
    Reverse = 2,  // makes the chain roll backwards for a moment
    Bomb = 3,     // also clears the balls next to the cleared group
    Aim = 4       // shows an aim guide for a while
}

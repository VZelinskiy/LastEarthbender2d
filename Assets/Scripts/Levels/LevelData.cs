using UnityEngine;

/// <summary>
/// Everything that makes one level different from another. Create new levels with
/// Assets > Create > Earthbender > Level Data and add them to the LevelController.
/// </summary>
[CreateAssetMenu(fileName = "Level", menuName = "Earthbender/Level Data")]
public class LevelData : ScriptableObject
{
    [SerializeField] string levelName = "New Level";

    [Header("Chain")]
    [SerializeField, Min(1)] int ballCount = 30;
    [SerializeField, Range(2, 5)] int ballTypeCount = 3;
    [SerializeField, Min(0.1f)] float chainSpeed = 1f;
    [SerializeField, Range(0f, 0.3f)] float bonusChance = 0.06f; // chance that a ball carries a bonus

    [Header("Spiral path")]
    [SerializeField, Min(1)] int spiralTurns = 3;
    [SerializeField] float spiralStartRadius = 4.5f;
    [SerializeField] float spiralEndRadius = 1.2f;

    public string LevelName => levelName;
    public int BallCount => ballCount;
    public int BallTypeCount => ballTypeCount;
    public float ChainSpeed => chainSpeed;
    public float BonusChance => bonusChance;
    public int SpiralTurns => spiralTurns;
    public float SpiralStartRadius => spiralStartRadius;
    public float SpiralEndRadius => spiralEndRadius;
}

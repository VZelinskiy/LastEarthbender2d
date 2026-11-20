using System;
using UnityEngine;

public enum GameState
{
    Playing,
    Won,
    Lost
}

/// <summary>
/// Runs the levels: loads a LevelData into the path and the chain, and decides when the
/// player has won (chain cleared) or lost (the head reached the end of the path).
/// </summary>
[DefaultExecutionOrder(20)] // after BallChain and Shooter have started
public class LevelController : MonoBehaviour
{
    [SerializeField] LevelData[] levels;

    BallChain chain;
    SpiralPathBuilder pathBuilder;
    PathSampler pathSampler;
    Shooter shooter;
    int levelIndex;

    public GameState State { get; private set; } = GameState.Playing;
    public event Action<GameState> StateChanged;

    public int LevelCount => levels.Length;
    public int LevelNumber => levelIndex + 1;
    public string CurrentLevelName => levels[levelIndex].LevelName;
    public int BallsRemaining => chain != null ? chain.BallsRemaining : 0;
    public bool HasNextLevel => levelIndex + 1 < levels.Length;

    void Start()
    {
        chain = FindAnyObjectByType<BallChain>();
        pathBuilder = FindAnyObjectByType<SpiralPathBuilder>();
        pathSampler = FindAnyObjectByType<PathSampler>();
        shooter = FindAnyObjectByType<Shooter>();

        bool hasLevels = levels != null && levels.Length > 0;
        if (!hasLevels || chain == null || pathBuilder == null || pathSampler == null || shooter == null)
        {
            Debug.LogError("LevelController: needs at least one LevelData plus a BallChain, a path and a Shooter in the scene.", this);
            enabled = false;
            return;
        }

        chain.ChainReachedEnd += OnChainReachedEnd;
        chain.ChainCleared += OnChainCleared;
        LoadLevel(0);
    }

    void OnDestroy()
    {
        if (chain == null) return;
        chain.ChainReachedEnd -= OnChainReachedEnd;
        chain.ChainCleared -= OnChainCleared;
    }

    public void LoadLevel(int index)
    {
        levelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        var level = levels[levelIndex];

        foreach (var projectile in FindObjectsByType<Projectile>())
            Destroy(projectile.gameObject);

        pathBuilder.Configure(level.SpiralTurns, level.SpiralStartRadius, level.SpiralEndRadius);
        pathSampler.Rebuild();
        chain.BeginLevel(level.BallCount, level.BallTypeCount, level.ChainSpeed, level.BonusChance);
        shooter.ResetShooter();

        SetState(GameState.Playing);
    }

    public void RestartLevel()
    {
        LoadLevel(levelIndex);
    }

    /// <summary>Loads the next level, or starts over from the first one after the last.</summary>
    public void NextLevel()
    {
        LoadLevel(HasNextLevel ? levelIndex + 1 : 0);
    }

    void SetState(GameState newState)
    {
        State = newState;
        shooter.InputEnabled = newState == GameState.Playing;
        StateChanged?.Invoke(newState);
    }

    void OnChainReachedEnd()
    {
        if (State == GameState.Playing) SetState(GameState.Lost);
    }

    void OnChainCleared()
    {
        if (State == GameState.Playing) SetState(GameState.Won);
    }
}

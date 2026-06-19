/// <summary>
/// Static bag of data written by GameManager before loading GameOver/Win
/// and read by GameOverUI / WinUI on the other side.
/// No MonoBehaviour needed - static fields survive scene loads.
/// </summary>
public static class GameResultData
{
    public static float FinalSanity   = 100f;
    public static float SurvivalTime  = 0f;
    public static int   EnemiesKilled = 0;
    public static LoseCause LoseCause = LoseCause.Sanity;
}

public enum LoseCause { Sanity, EnemyContact }

/// <summary>
/// Central place for scene names - keeps every SceneManager.LoadScene() call
/// in sync and makes typo-errors a compile error rather than a runtime crash.
/// </summary>
public static class SceneNames
{
    public const string MainMenu       = "MainMenu";
    public const string EyeCalibration = "GazeCalibration";
    public const string DemoLevel      = "GameScene";
    public const string GameOver       = "GameOver";
    public const string Win            = "Win";
}

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game state machine for the DemoLevel scene.
/// On win or lose, populates GameResultData and loads the correct result scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { Intro, Playing, Ended }
    public State CurrentState { get; private set; } = State.Intro;

    [Header("References")]
    public GazeTracker    gazeTracker;
    public SanityManager  sanityManager;

    [Header("UI")]
    [Tooltip("'Press SPACE to start' overlay shown before the game begins.")]
    public GameObject introPanel;

    // Stat tracking
    private float _playTime     = 0f;
    public  int   EnemiesKilled { get; private set; } = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetState(State.Intro);
    }

    private void Update()
    {
        if (CurrentState == State.Playing)
            _playTime += Time.deltaTime;

        if (CurrentState == State.Intro && Input.GetKeyDown(KeyCode.Space))
        {
            if (gazeTracker != null && gazeTracker.mode == GazeTracker.Mode.Webcam)
                gazeTracker.Calibrate();

            SetState(State.Playing);
        }
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>Called by EnemyAI.Die().</summary>
    public void NotifyEnemyKilled() => EnemiesKilled++;

    public void TriggerWin()
    {
        if (CurrentState != State.Playing) return;
        SetState(State.Ended);
        GameSessionTimer.StopAndFreeze();
        GameSessionTimer.CopyToResultData();
        EnemyKillTracker.CopyToResultData();
        PopulateResult(LoseCause.Sanity); // cause unused for win
        SceneManager.LoadScene(SceneNames.Win);
    }

    public void TriggerLose(LoseCause cause = LoseCause.EnemyContact)
    {
        if (CurrentState != State.Playing) return;
        SetState(State.Ended);
        GameSessionTimer.StopAndFreeze();
        GameSessionTimer.CopyToResultData();
        EnemyKillTracker.CopyToResultData();
        GameResultData.LoseCause = cause;
        PopulateResult(cause);
        SceneManager.LoadScene(SceneNames.GameOver);
    }

    // ------------------------------------------------------------------
    // Private
    // ------------------------------------------------------------------

    private void SetState(State newState)
    {
        CurrentState = newState;

        if (newState == State.Playing)
        {
            EnemiesKilled = 0;
            EnemyKillTracker.Reset();
            GameSessionTimer.StartNewRun();
        }

        if (introPanel != null)
            introPanel.SetActive(newState == State.Intro);
    }

    private void PopulateResult(LoseCause cause)
    {
        GameResultData.FinalSanity   = sanityManager != null ? sanityManager.CurrentSanity : 0f;
        GameResultData.SurvivalTime  = Mathf.Max(_playTime, GameSessionTimer.GetElapsedSeconds());
        GameResultData.EnemiesKilled = Mathf.Max(EnemiesKilled, EnemyKillTracker.GetKills());
        GameResultData.LoseCause     = cause;
    }
}

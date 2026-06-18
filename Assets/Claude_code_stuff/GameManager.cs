using UnityEngine;

/// <summary>
/// Simple state machine. Wire up the UI panels (calibration, game over,
/// victory) from the Inspector.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum State { Calibration, Playing, Won, Lost }
    public State CurrentState { get; private set; } = State.Calibration;

    [Header("References")]
    public GazeTracker gazeTracker;

    [Header("UI Panels (optional, assigned from the Inspector)")]
    public GameObject calibrationPanel;
    public GameObject gameOverPanel;
    public GameObject winPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetState(State.Calibration);
    }

    private void Update()
    {
        if (CurrentState == State.Calibration && Input.GetKeyDown(KeyCode.Space))
        {
            if (gazeTracker != null && gazeTracker.mode == GazeTracker.Mode.Webcam)
                gazeTracker.Calibrate();

            SetState(State.Playing);
        }
    }

    public void TriggerWin()
    {
        if (CurrentState != State.Playing) return;
        SetState(State.Won);
    }

    public void TriggerLose()
    {
        if (CurrentState != State.Playing) return;
        SetState(State.Lost);
    }

    private void SetState(State newState)
    {
        CurrentState = newState;

        if (calibrationPanel != null) calibrationPanel.SetActive(newState == State.Calibration);
        if (gameOverPanel != null) gameOverPanel.SetActive(newState == State.Lost);
        if (winPanel != null) winPanel.SetActive(newState == State.Won);

        Time.timeScale = (newState == State.Won || newState == State.Lost) ? 0f : 1f;
    }

    /// <summary>Wire this up to the "Retry" button on the end screens.</summary>
    public void Retry()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}

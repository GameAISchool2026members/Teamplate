using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the GameOver scene.
/// Reads final stats from the static GameResultData class that
/// GameManager populates before loading this scene.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Title")]
    [Tooltip("'SANITY LOST' or 'DEVOURED' depending on cause of death.")]
    public Text causeText;

    [Header("Stats")]
    public Text sanityText;
    public Text timeText;
    public Text enemiesText;

    [Header("Buttons")]
    public Button retryBtn;
    public Button mainMenuBtn;

    private void Start()
    {
        // Populate cause
        if (causeText)
            causeText.text = GameResultData.LoseCause == LoseCause.Sanity
                ? "SANITY LOST"
                : "DEVOURED";

        // Populate stats
        if (sanityText)
            sanityText.text = Mathf.RoundToInt(GameResultData.FinalSanity) + "%";

        if (timeText)
            timeText.text = FormatTime(GameResultData.SurvivalTime);

        if (enemiesText)
            enemiesText.text = GameResultData.EnemiesKilled.ToString();

        // Wire buttons
        if (retryBtn)    retryBtn.onClick.AddListener(OnRetry);
        if (mainMenuBtn) mainMenuBtn.onClick.AddListener(OnMainMenu);
    }

    public void OnRetry()    => SceneManager.LoadScene(SceneNames.DemoLevel);
    public void OnMainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m}:{s:00}";
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the Win scene.
/// Reads final stats from the static GameResultData class that
/// GameManager populates before loading this scene.
/// </summary>
public class WinUI : MonoBehaviour
{
    [Header("Stats")]
    public Text  sanityText;
    public Text  timeText;
    public Text  enemiesText;

    [Header("Sanity bar (optional visual feedback)")]
    [Tooltip("Set Image type to Filled (Horizontal) and assign here.")]
    public Image sanityBarFill;

    [Header("Buttons")]
    public Button playAgainBtn;
    public Button mainMenuBtn;

    private void Start()
    {
        float sanityPct = Mathf.Clamp01(GameResultData.FinalSanity / 100f);

        if (sanityText)
            sanityText.text = Mathf.RoundToInt(GameResultData.FinalSanity) + "%";

        if (timeText)
            timeText.text = FormatTime(GameResultData.SurvivalTime);

        if (enemiesText)
            enemiesText.text = GameResultData.EnemiesKilled + " / 4";

        if (sanityBarFill)
            sanityBarFill.fillAmount = sanityPct;

        if (playAgainBtn) playAgainBtn.onClick.AddListener(OnPlayAgain);
        if (mainMenuBtn)  mainMenuBtn.onClick.AddListener(OnMainMenu);
    }

    public void OnPlayAgain() => SceneManager.LoadScene(SceneNames.DemoLevel);
    public void OnMainMenu()  => SceneManager.LoadScene(SceneNames.MainMenu);

    private static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m}:{s:00}";
    }
}

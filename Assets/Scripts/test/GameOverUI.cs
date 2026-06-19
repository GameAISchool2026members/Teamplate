using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Drives the GameOver scene.
/// Reads final stats from the static GameResultData class that
/// GameManager populates before loading this scene.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Title")]
    [Tooltip("Main game over title text.")]
    public Text causeText;

    [Header("Stats")]
    public Text sanityText;
    public Text timeText;
    public Text enemiesText;

    [Header("Buttons")]
    public Button retryBtn;
    public Button mainMenuBtn;

    [Header("Audio")]
    [Tooltip("Sound played once when the GameOver scene opens.")]
    public AudioClip gameOverClip;
    public AudioSource gameOverAudioSource;
    [Range(0f, 1f)]
    public float gameOverVolume = 0.5f;

    private void Start()
    {
        PlayGameOverSound();

        // Populate title
        if (causeText)
            causeText.text = "YOU DIED";

        // Populate stats
        if (sanityText)
            sanityText.text = Mathf.RoundToInt(GameResultData.FinalSanity) + "%";

        if (timeText)
        {
            float elapsedSeconds = GameResultData.SurvivalTime > 0f
                ? GameResultData.SurvivalTime
                : GameSessionTimer.GetElapsedSeconds();

            timeText.text = FormatSeconds(elapsedSeconds);
        }

        if (enemiesText)
            enemiesText.text = Mathf.Max(GameResultData.EnemiesKilled, EnemyKillTracker.GetKills()).ToString();

        // Wire buttons
        if (retryBtn)    retryBtn.onClick.AddListener(OnRetry);
        if (mainMenuBtn) mainMenuBtn.onClick.AddListener(OnMainMenu);
    }

    public void OnRetry()    => SceneManager.LoadScene(SceneNames.DemoLevel);
    public void OnMainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);

    private static string FormatSeconds(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return totalSeconds + " s";
    }

    private void PlayGameOverSound()
    {
        AudioClip clipToPlay = ResolveGameOverClip();
        if (clipToPlay == null)
            return;

        AudioSource source = gameOverAudioSource;
        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.volume = Mathf.Clamp01(gameOverVolume);
        source.clip = clipToPlay;
        source.Play();
    }

    private AudioClip ResolveGameOverClip()
    {
        if (gameOverClip != null)
            return gameOverClip;

        AudioClip clip = Resources.Load<AudioClip>("Audio/game-over-sound");
        if (clip != null)
            return clip;

        clip = Resources.Load<AudioClip>("game-over-sound");
        if (clip != null)
            return clip;

#if UNITY_EDITOR
        clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/game-over-sound.mp3");
        if (clip != null)
            return clip;
#endif

        return null;
    }
}

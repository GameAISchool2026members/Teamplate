using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Audio")]
    [Tooltip("Sound played once when the Win scene opens.")]
    public AudioClip winClip;
    public AudioSource winAudioSource;
    [Range(0f, 1f)]
    public float winVolume = 0.5f;

    private void Start()
    {
        PlayWinSound();

        float sanityPct = Mathf.Clamp01(GameResultData.FinalSanity / 100f);

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

        if (sanityBarFill)
            sanityBarFill.fillAmount = sanityPct;

        if (playAgainBtn) playAgainBtn.onClick.AddListener(OnPlayAgain);
        if (mainMenuBtn)  mainMenuBtn.onClick.AddListener(OnMainMenu);
    }

    public void OnPlayAgain() => SceneManager.LoadScene(SceneNames.DemoLevel);
    public void OnMainMenu()  => SceneManager.LoadScene(SceneNames.MainMenu);

    private static string FormatSeconds(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return totalSeconds + " s";
    }

    private void PlayWinSound()
    {
        AudioClip clipToPlay = ResolveWinClip();
        if (clipToPlay == null)
            return;

        AudioSource source = winAudioSource;
        if (source == null)
            source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.volume = Mathf.Clamp01(winVolume);
        source.clip = clipToPlay;
        source.Play();
    }

    private AudioClip ResolveWinClip()
    {
        if (winClip != null)
            return winClip;

        AudioClip clip = Resources.Load<AudioClip>("Audio/win-sound");
        if (clip != null)
            return clip;

        clip = Resources.Load<AudioClip>("win-sound");
        if (clip != null)
            return clip;

#if UNITY_EDITOR
        clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/win-sound.mp3");
        if (clip != null)
            return clip;
#endif

        return null;
    }
}

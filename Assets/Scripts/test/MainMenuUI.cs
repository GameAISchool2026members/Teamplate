using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the MainMenu scene.
/// SceneBuilder assigns all public references; this script wires up
/// all interactive events in Start() at runtime.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector references (assigned by SceneBuilder)
    // ------------------------------------------------------------------

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;

    [Header("Main menu buttons")]
    public Button startGameBtn;
    public Button optionsBtn;
    public Button eyeGazingBtn;

    [Header("Options - cycling controls")]
    public Text displayModeValue;
    public Button displayModeLeft;
    public Button displayModeRight;

    public Text resolutionValue;
    public Button resolutionLeft;
    public Button resolutionRight;

    [Header("Options - volume cycling")]
    public Text masterVolumeValue;
    public Button masterVolumeLeft;
    public Button masterVolumeRight;

    public Text sfxVolumeValue;
    public Button sfxVolumeLeft;
    public Button sfxVolumeRight;

    public Text musicVolumeValue;
    public Button musicVolumeLeft;
    public Button musicVolumeRight;

    [Header("Options - toggles")]
    public Button vsyncToggleBtn;
    public Text   vsyncValueText;
    public Button showFPSToggleBtn;
    public Text   showFPSValueText;

    [Header("Back button (inside options panel)")]
    public Button optionsBackBtn;

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------

    private static readonly string[] DisplayModes =
        { "FULLSCREEN", "WINDOWED", "BORDERLESS" };
    private static readonly FullScreenMode[] FullScreenModes =
        { FullScreenMode.ExclusiveFullScreen, FullScreenMode.Windowed, FullScreenMode.FullScreenWindow };

    private static readonly string[] Resolutions =
        { "1920 x 1080", "1280 x 720", "2560 x 1440", "3840 x 2160" };
    private static readonly (int w, int h)[] ResolutionValues =
        { (1920, 1080), (1280, 720), (2560, 1440), (3840, 2160) };

    private static readonly int[] VolumeLevels = { 0, 20, 40, 60, 80, 100 };

    private int _displayModeIdx;
    private int _resolutionIdx;
    private int _masterVol;
    private int _sfxVol;
    private int _musicVol;
    private bool _vsync;
    private bool _showFPS;

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    private void Start()
    {
        LoadPrefs();
        RefreshAllDisplays();
        WireButtons();
    }

    // ------------------------------------------------------------------
    // Navigation
    // ------------------------------------------------------------------

    public void OnStartGame()   => SceneManager.LoadScene(SceneNames.DemoLevel);
    public void OnEyeGazing()   => SceneManager.LoadScene(SceneNames.EyeCalibration);

    public void OnOptions()
    {
        if (mainPanel)    mainPanel.SetActive(false);
        if (optionsPanel) optionsPanel.SetActive(true);
    }

    public void OnOptionsBack()
    {
        if (optionsPanel) optionsPanel.SetActive(false);
        if (mainPanel)    mainPanel.SetActive(true);
    }

    // ------------------------------------------------------------------
    // Cycling options (called by SceneBuilder persistent listeners)
    // ------------------------------------------------------------------

    public void CycleDisplayModeLeft()  => CycleDisplayMode(-1);
    public void CycleDisplayModeRight() => CycleDisplayMode(+1);
    public void CycleResolutionLeft()   => CycleResolution(-1);
    public void CycleResolutionRight()  => CycleResolution(+1);
    public void CycleMasterVolLeft()    => CycleVolume(ref _masterVol, -1, "MasterVolume", masterVolumeValue);
    public void CycleMasterVolRight()   => CycleVolume(ref _masterVol, +1, "MasterVolume", masterVolumeValue);
    public void CycleSFXVolLeft()       => CycleVolume(ref _sfxVol,    -1, "SFXVolume",    sfxVolumeValue);
    public void CycleSFXVolRight()      => CycleVolume(ref _sfxVol,    +1, "SFXVolume",    sfxVolumeValue);
    public void CycleMusicVolLeft()     => CycleVolume(ref _musicVol,  -1, "MusicVolume",  musicVolumeValue);
    public void CycleMusicVolRight()    => CycleVolume(ref _musicVol,  +1, "MusicVolume",  musicVolumeValue);
    public void ToggleVSync()           { _vsync = !_vsync;   ApplyVSync();   SavePrefs(); RefreshAllDisplays(); }
    public void ToggleShowFPS()         { _showFPS = !_showFPS; SavePrefs(); RefreshAllDisplays(); }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void CycleDisplayMode(int dir)
    {
        _displayModeIdx = Mod(_displayModeIdx + dir, DisplayModes.Length);
        Screen.fullScreenMode = FullScreenModes[_displayModeIdx];
        PlayerPrefs.SetInt("DisplayMode", _displayModeIdx);
        RefreshAllDisplays();
    }

    private void CycleResolution(int dir)
    {
        _resolutionIdx = Mod(_resolutionIdx + dir, Resolutions.Length);
        var (w, h) = ResolutionValues[_resolutionIdx];
        Screen.SetResolution(w, h, Screen.fullScreenMode);
        PlayerPrefs.SetInt("Resolution", _resolutionIdx);
        RefreshAllDisplays();
    }

    private void CycleVolume(ref int field, int dir, string key, Text display)
    {
        int idx = System.Array.IndexOf(VolumeLevels, field);
        idx = Mod(idx + dir, VolumeLevels.Length);
        field = VolumeLevels[idx];
        PlayerPrefs.SetInt(key, field);
        if (key == "MasterVolume") AudioListener.volume = field / 100f;
        if (display) display.text = field + "%";
    }

    private void ApplyVSync()
    {
        QualitySettings.vSyncCount = _vsync ? 1 : 0;
        PlayerPrefs.SetInt("VSync", _vsync ? 1 : 0);
    }

    private void LoadPrefs()
    {
        _displayModeIdx = PlayerPrefs.GetInt("DisplayMode", 0);
        _resolutionIdx  = PlayerPrefs.GetInt("Resolution",  0);
        _masterVol      = PlayerPrefs.GetInt("MasterVolume", 80);
        _sfxVol         = PlayerPrefs.GetInt("SFXVolume",   100);
        _musicVol       = PlayerPrefs.GetInt("MusicVolume",  60);
        _vsync          = PlayerPrefs.GetInt("VSync", 1) == 1;
        _showFPS        = PlayerPrefs.GetInt("ShowFPS", 0) == 1;

        AudioListener.volume = _masterVol / 100f;
        QualitySettings.vSyncCount = _vsync ? 1 : 0;
    }

    private void SavePrefs()
    {
        PlayerPrefs.SetInt("VSync",    _vsync   ? 1 : 0);
        PlayerPrefs.SetInt("ShowFPS",  _showFPS ? 1 : 0);
    }

    private void RefreshAllDisplays()
    {
        SetText(displayModeValue,  DisplayModes[_displayModeIdx]);
        SetText(resolutionValue,   Resolutions[_resolutionIdx]);
        SetText(masterVolumeValue, _masterVol + "%");
        SetText(sfxVolumeValue,    _sfxVol    + "%");
        SetText(musicVolumeValue,  _musicVol  + "%");
        SetText(vsyncValueText,    _vsync   ? "ON" : "OFF");
        SetText(showFPSValueText,  _showFPS ? "ON" : "OFF");
    }

    private void WireButtons()
    {
        Wire(startGameBtn,       OnStartGame);
        Wire(optionsBtn,         OnOptions);
        Wire(eyeGazingBtn,       OnEyeGazing);
        Wire(optionsBackBtn,     OnOptionsBack);
        Wire(displayModeLeft,    CycleDisplayModeLeft);
        Wire(displayModeRight,   CycleDisplayModeRight);
        Wire(resolutionLeft,     CycleResolutionLeft);
        Wire(resolutionRight,    CycleResolutionRight);
        Wire(masterVolumeLeft,   CycleMasterVolLeft);
        Wire(masterVolumeRight,  CycleMasterVolRight);
        Wire(sfxVolumeLeft,      CycleSFXVolLeft);
        Wire(sfxVolumeRight,     CycleSFXVolRight);
        Wire(musicVolumeLeft,    CycleMusicVolLeft);
        Wire(musicVolumeRight,   CycleMusicVolRight);
        Wire(vsyncToggleBtn,     ToggleVSync);
        Wire(showFPSToggleBtn,   ToggleShowFPS);
    }

    private static void Wire(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn != null) btn.onClick.AddListener(action);
    }

    private static void SetText(Text t, string value)
    {
        if (t != null) t.text = value;
    }

    private static int Mod(int x, int m) => ((x % m) + m) % m;
}

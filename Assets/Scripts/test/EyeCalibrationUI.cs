using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the EyeCalibration scene.
/// Shows a webcam preview, lets the player tune head-tracking sensitivity,
/// saves settings to PlayerPrefs, then returns to MainMenu.
/// </summary>
public class EyeCalibrationUI : MonoBehaviour
{
    [Header("References")]
    public RawImage  webcamPreview;
    public Slider    sensitivitySlider;
    public Text      sensitivityValueText;
    public Text      statusText;
    public Button    calibrateBtn;
    public Button    backBtn;

    [Header("Status messages")]
    public string msgReady   = "WEBCAM DETECTED  -  READY";
    public string msgNoCamera = "NO WEBCAM FOUND  -  USING MOUSE MODE";

    private WebCamTexture _webcam;

    private const string KeySensitivity = "GazeSensitivity";

    private void Start()
    {
        // Restore saved sensitivity (0-1 range)
        float saved = PlayerPrefs.GetFloat(KeySensitivity, 0.65f);
        if (sensitivitySlider)
        {
            sensitivitySlider.value = saved;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }
        UpdateSensitivityLabel(saved);

        // Try to start webcam preview
        if (WebCamTexture.devices.Length > 0)
        {
            _webcam = new WebCamTexture(WebCamTexture.devices[0].name, 160, 120, 20);
            _webcam.Play();
            if (webcamPreview) webcamPreview.texture = _webcam;
            SetStatus(msgReady, new Color(0.2f, 0.75f, 0.4f));
        }
        else
        {
            SetStatus(msgNoCamera, new Color(0.8f, 0.4f, 0.2f));
        }

        if (calibrateBtn) calibrateBtn.onClick.AddListener(OnCalibrate);
        if (backBtn)      backBtn.onClick.AddListener(OnBack);
    }

    private void OnSensitivityChanged(float val)
    {
        PlayerPrefs.SetFloat(KeySensitivity, val);
        UpdateSensitivityLabel(val);
    }

    private void UpdateSensitivityLabel(float val)
    {
        if (sensitivityValueText)
            sensitivityValueText.text = Mathf.RoundToInt(val * 100f) + "%";
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText)
        {
            statusText.text  = msg;
            statusText.color = color;
        }
    }

    public void OnCalibrate()
    {
        // GazeTracker will read GazeSensitivity from PlayerPrefs on scene load.
        // Mark that a real calibration was completed so DemoLevel knows
        // it can start in Webcam mode.
        PlayerPrefs.SetInt("GazeCalibrated", 1);
        GoToMainMenu();
    }

    public void OnBack() => GoToMainMenu();

    private void GoToMainMenu()
    {
        StopWebcam();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    private void StopWebcam()
    {
        if (_webcam != null && _webcam.isPlaying) _webcam.Stop();
    }

    private void OnDestroy() => StopWebcam();
}

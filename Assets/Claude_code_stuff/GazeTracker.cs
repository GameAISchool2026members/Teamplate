using UnityEngine;

/// <summary>
/// Exposes the "looked at" position on screen (ScreenPosition, in pixels).
/// Two interchangeable modes:
///  - Mouse: uses the pointer. FALLBACK and development mode: always start
///    here to build and test the rest of the game.
///  - Webcam: head-tracking via a calibrated skin-color centroid.
///    This is NOT real eye-tracking (which would require complex
///    calibration and external libraries) but a robust, fast-to-implement
///    proxy: the position of the head in the webcam frame moves the
///    vision circle on screen.
/// </summary>
public class GazeTracker : MonoBehaviour
{
    public enum Mode { Mouse, Webcam }

    [Header("Mode")]
    public Mode mode = Mode.Mouse;

    [Header("Webcam")]
    public int requestedWidth = 160;
    public int requestedHeight = 120;
    public int requestedFps = 20;
    [Tooltip("Analyze 1 pixel every N to save CPU. 2-4 works well for 160x120.")]
    public int sampleStep = 2;
    [Tooltip("Minimum number of matching 'skin' pixels required for a valid detection.")]
    public int minMatchingPixels = 15;

    [Header("Sensitivity / screen mapping")]
    [Tooltip("How far the circle moves on screen (in pixels) per unit of normalized head movement in the webcam frame.")]
    public float sensitivity = 2500f;
    public bool invertX = false;
    public bool invertY = false;
    [Tooltip("Movement smoothing, higher = more responsive.")]
    public float smoothSpeed = 8f;

    [Header("Debug")]
    public bool isCalibrated = false;

    public Vector2 ScreenPosition { get; private set; }

    private WebCamTexture _webcamTexture;
    private Color32[] _pixelBuffer;

    // Calibration reference values
    private float _calH, _calS, _calV;
    private Vector2 _calCentroidNorm; // normalized centroid (0-1) at calibration time
    private Vector2 _lastValidCentroidNorm;
    private Vector2 _currentScreenTarget;

    private void Start()
    {
        ScreenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        _currentScreenTarget = ScreenPosition;
        _lastValidCentroidNorm = new Vector2(0.5f, 0.5f);

        if (mode == Mode.Webcam)
            StartWebcam();
    }

    public void StartWebcam()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogWarning("No webcam found, falling back to Mouse mode.");
            mode = Mode.Mouse;
            return;
        }

        _webcamTexture = new WebCamTexture(WebCamTexture.devices[0].name, requestedWidth, requestedHeight, requestedFps);
        _webcamTexture.Play();
    }

    /// <summary>
    /// Call this when the player looks straight at the webcam (calibration
    /// screen). Samples a small central region of the frame to get the
    /// skin color and the reference center (= "looking neutral/center").
    /// </summary>
    public void Calibrate()
    {
        if (_webcamTexture == null || !_webcamTexture.isPlaying) return;

        int w = _webcamTexture.width;
        int h = _webcamTexture.height;
        Color32[] pixels = _webcamTexture.GetPixels32();

        int boxSize = Mathf.Min(w, h) / 6; // small central region
        int cx = w / 2;
        int cy = h / 2;

        float sumH = 0, sumS = 0, sumV = 0;
        int count = 0;

        for (int y = cy - boxSize; y < cy + boxSize; y++)
        {
            for (int x = cx - boxSize; x < cx + boxSize; x++)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) continue;
                Color c = pixels[y * w + x];
                Color.RGBToHSV(c, out float hh, out float ss, out float vv);
                sumH += hh; sumS += ss; sumV += vv;
                count++;
            }
        }

        if (count == 0) return;

        _calH = sumH / count;
        _calS = sumS / count;
        _calV = sumV / count;
        _calCentroidNorm = new Vector2(0.5f, 0.5f); // by definition, frame center = neutral position
        _lastValidCentroidNorm = _calCentroidNorm;
        isCalibrated = true;

        Debug.Log($"Calibration complete. H={_calH:F2} S={_calS:F2} V={_calV:F2}");
    }

    private void Update()
    {
        if (mode == Mode.Mouse)
        {
            _currentScreenTarget = Input.mousePosition;
        }
        else
        {
            UpdateWebcamTracking();
        }

        ScreenPosition = Vector2.Lerp(ScreenPosition, _currentScreenTarget, smoothSpeed * Time.deltaTime);
    }

    private void UpdateWebcamTracking()
    {
        if (_webcamTexture == null || !_webcamTexture.isPlaying || !isCalibrated) return;
        if (!_webcamTexture.didUpdateThisFrame) return;

        int w = _webcamTexture.width;
        int h = _webcamTexture.height;
        _pixelBuffer = _webcamTexture.GetPixels32();

        // Tolerance range around the calibrated skin color
        float hueTol = 0.06f;
        float satMin = Mathf.Max(0f, _calS * 0.5f);
        float satMax = Mathf.Min(1f, _calS * 1.6f);
        float valMin = Mathf.Max(0f, _calV * 0.5f);
        float valMax = Mathf.Min(1f, _calV * 1.6f);

        long sumX = 0, sumY = 0;
        int matchCount = 0;

        for (int y = 0; y < h; y += sampleStep)
        {
            for (int x = 0; x < w; x += sampleStep)
            {
                Color c = _pixelBuffer[y * w + x];
                Color.RGBToHSV(c, out float hh, out float ss, out float vv);

                if (Mathf.Abs(hh - _calH) <= hueTol && ss >= satMin && ss <= satMax && vv >= valMin && vv <= valMax)
                {
                    sumX += x;
                    sumY += y;
                    matchCount++;
                }
            }
        }

        Vector2 centroidNorm;
        if (matchCount >= minMatchingPixels)
        {
            centroidNorm = new Vector2((float)sumX / matchCount / w, (float)sumY / matchCount / h);
            _lastValidCentroidNorm = centroidNorm;
        }
        else
        {
            // No valid detection: keep the last known position instead of
            // letting the vision circle jump around randomly.
            centroidNorm = _lastValidCentroidNorm;
        }

        Vector2 delta = centroidNorm - _calCentroidNorm;
        if (invertX) delta.x = -delta.x;
        if (invertY) delta.y = -delta.y;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        _currentScreenTarget = screenCenter + delta * sensitivity;

        _currentScreenTarget.x = Mathf.Clamp(_currentScreenTarget.x, 0, Screen.width);
        _currentScreenTarget.y = Mathf.Clamp(_currentScreenTarget.y, 0, Screen.height);
    }

    private void OnDestroy()
    {
        if (_webcamTexture != null && _webcamTexture.isPlaying)
            _webcamTexture.Stop();
    }
}

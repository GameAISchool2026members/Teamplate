using UnityEngine;
using UnitEye;

public class GazeTracker : MonoBehaviour
{
    public enum Mode
    {
        Mouse,
        Webcam
    }

    private const string KeyCalibrated = "GazeCalibrated";

    [SerializeField]
    public Mode mode = Mode.Mouse;

    private void Awake()
    {
        if (PlayerPrefs.GetInt(KeyCalibrated, 0) == 1)
            mode = Mode.Webcam;
    }

    public void Calibrate()
    {
        mode = Mode.Webcam;
        PlayerPrefs.SetInt(KeyCalibrated, 1);
        PlayerPrefs.Save();
    }
}

public class VisionController : MonoBehaviour
{
    protected FlashlightFollow flashlightFollow;

    protected virtual void Awake()
    {
        flashlightFollow = GetComponent<FlashlightFollow>();
    }

    public virtual Vector2 WorldPosition => transform.position;

    public virtual float Radius
    {
        get
        {
            if (flashlightFollow == null)
                flashlightFollow = GetComponent<FlashlightFollow>();

            if (flashlightFollow == null)
                return 0f;

            return flashlightFollow.worldRadius > 0f
                ? flashlightFollow.worldRadius
                : flashlightFollow.GetWorldRadius();
        }
    }
}

public class FlashlightController : VisionController
{
    [Header("References")]
    public GazeTracker gazeTracker;
    public Camera mainCamera;
    public Transform visionLight;
    public Gaze gazeComponent;

    [Header("Shape")]
    public float visionRadius = 2.2f;

    private void Start()
    {
        SyncFlashlightFollow();
    }

    private void LateUpdate()
    {
        SyncFlashlightFollow();
    }

    protected override void Awake()
    {
        base.Awake();

        flashlightFollow = GetComponent<FlashlightFollow>();
        if (flashlightFollow == null)
            flashlightFollow = gameObject.AddComponent<FlashlightFollow>();
    }

    private void SyncFlashlightFollow()
    {
        if (flashlightFollow == null)
            flashlightFollow = GetComponent<FlashlightFollow>();

        if (flashlightFollow == null)
            return;

        bool useEyeGaze = gazeTracker != null && gazeTracker.mode == GazeTracker.Mode.Webcam;
        flashlightFollow.useEyeGaze = useEyeGaze;

        if (useEyeGaze && flashlightFollow.gazeComponent == null)
            flashlightFollow.gazeComponent = gazeComponent != null ? gazeComponent : Object.FindObjectOfType<Gaze>();

        if (visionRadius > 0f)
            flashlightFollow.holeRadius = WorldRadiusToNormalizedRadius(visionRadius);
    }

    private float WorldRadiusToNormalizedRadius(float desiredWorldRadius)
    {
        Camera cameraRef = mainCamera != null ? mainCamera : Camera.main;
        if (cameraRef == null)
            return desiredWorldRadius;

        if (!cameraRef.orthographic)
            return desiredWorldRadius;

        float worldHeight = cameraRef.orthographicSize * 2f;
        float worldWidth = worldHeight * Screen.width / Screen.height;
        float minDimension = Mathf.Min(worldWidth, worldHeight);

        if (minDimension <= 0f)
            return desiredWorldRadius;

        return desiredWorldRadius / (minDimension * 0.5f);
    }
}
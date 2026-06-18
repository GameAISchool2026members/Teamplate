using UnityEngine;

/// <summary>
/// Converts the screen position provided by GazeTracker into a world
/// position, and applies it to the GameObject holding the circular light
/// (Light2D of type Point/Parametric, see GDD section 4.1).
/// Exposes WorldPosition and Radius for SanityManager and EnemyAI.
/// </summary>
public class VisionController : MonoBehaviour
{
    [Header("References")]
    public GazeTracker gazeTracker;
    public Camera mainCamera;
    [Tooltip("The GameObject with the Light2D component representing the vision circle.")]
    public Transform visionLight;

    [Header("Parameters")]
    public float visionRadius = 2.2f;
    public float smoothSpeed = 10f;

    public Vector3 WorldPosition => visionLight.position;
    public float Radius => visionRadius;

    private void Reset()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (gazeTracker == null || visionLight == null) return;

        Vector2 screenPos = gazeTracker.ScreenPosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        worldPos.z = visionLight.position.z; // keep the light's original z

        visionLight.position = Vector3.Lerp(visionLight.position, worldPos, smoothSpeed * Time.deltaTime);
    }
}

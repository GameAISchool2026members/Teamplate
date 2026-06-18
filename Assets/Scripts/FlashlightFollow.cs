using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnitEye;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class FlashlightFollow : MonoBehaviour
{
    [Header("Flashlight Material")]
    public Material flashlightMaterial;

    [Header("Flashlight Shape")]
    [Tooltip("Normalized radius of the visible flashlight hole in UV space.")]
    [Range(0f, 1f)]
    public float holeRadius = 0.18f;

    [Header("Input Mode")]
    [Tooltip("Use eye gaze instead of mouse to control the flashlight.")]
    public bool useEyeGaze = false;
    [Tooltip("Key to toggle between mouse and eye gaze at runtime.")]
    public Key toggleKey = Key.Tab;
    [Tooltip("Reference to the UnitEye Gaze component in the scene.")]
    public Gaze gazeComponent;

    [Header("Collision Detection")]
    [Tooltip("Layer mask for objects that should be detected inside the flashlight hole.")]
    public LayerMask detectionMask = ~0;

    [Header("Debug Info")]
    [Tooltip("Current input position in screen pixels.")]
    public Vector2 mouseScreenPosition;
    [Tooltip("Current flashlight center in UV coordinates.")]
    public Vector2 holeCenterUV;
    [Tooltip("Current world radius of the flashlight collision shape.")]
    public float worldRadius;

    private CircleCollider2D circleCollider;
    private Camera mainCamera;

    private readonly HashSet<Collider2D> overlappingColliders = new HashSet<Collider2D>();

    public bool IsSomethingInside => overlappingColliders.Count > 0;
    public int insideCount => overlappingColliders.Count;

    void Awake()
    {
        mainCamera = Camera.main;
        circleCollider = GetComponent<CircleCollider2D>();

        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
            circleCollider.offset = Vector2.zero;
            circleCollider.radius = WorldRadiusFromNormalizedRadius(holeRadius);
        }

        Rigidbody2D body2D = GetComponent<Rigidbody2D>();
        if (body2D != null)
        {
            body2D.bodyType = RigidbodyType2D.Kinematic;
            body2D.simulated = true;
            body2D.gravityScale = 0f;
        }
    }

    public float GetWorldRadius()
    {
        return WorldRadiusFromNormalizedRadius(holeRadius);
    }

    void Update()
    {
        if (Keyboard.current[toggleKey].wasPressedThisFrame)
            useEyeGaze = !useEyeGaze;

        UpdateInputPosition();
        UpdateFlashlightMaterial();
        UpdateCollisionShape();
        DetectOverlappingObjects();
    }

    void UpdateInputPosition()
    {
        if (useEyeGaze && gazeComponent != null)
        {
            // gazeLocation is in screen pixels with (0,0) at top-left (Unity GUI space)
            // ScreenToWorldPoint expects (0,0) at bottom-left, so flip Y
            Vector2 gazePixels = gazeComponent.gazeLocation;
            Debug.Log(gazePixels);
            mouseScreenPosition = new Vector2(gazePixels.x, Screen.height - gazePixels.y);
        }
        else
        {
            Debug.Log($"No gaze");
            mouseScreenPosition = Mouse.current.position.ReadValue();
        }

        holeCenterUV = new Vector2(
            mouseScreenPosition.x / Screen.width,
            mouseScreenPosition.y / Screen.height
        );
    }

    void UpdateFlashlightMaterial()
    {
        if (flashlightMaterial == null) return;

        // Calculate aspect ratio correction
        float aspectRatio = (float)Screen.width / Screen.height;
        
        // Correct HoleCenter X by multiplying by aspect ratio to compensate for shader division
        Vector2 correctedCenter = new Vector2(
            holeCenterUV.x * aspectRatio,
            holeCenterUV.y
        );

        // Correct radius by multiplying by aspect ratio to maintain circle shape
        float correctedRadius = holeRadius * aspectRatio;

        if (flashlightMaterial.HasProperty("_HoleCenter"))
            flashlightMaterial.SetVector("_HoleCenter", correctedCenter);

        if (flashlightMaterial.HasProperty("_HoleRadius"))
            flashlightMaterial.SetFloat("_HoleRadius", correctedRadius);

        // Pass screen aspect ratio to shader for coordinate correction
        if (flashlightMaterial.HasProperty("_ScreenAspectRatio"))
        {
            flashlightMaterial.SetFloat("_ScreenAspectRatio", aspectRatio);
        }
    }

    void UpdateCollisionShape()
    {
        if (mainCamera == null || circleCollider == null) return;

        float worldZ = -mainCamera.transform.position.z;
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, worldZ));
        worldPosition.z = 0f;
        transform.position = worldPosition;

        // Apply aspect ratio correction to radius for collision shape (must match shader)
        float aspectRatio = (float)Screen.width / Screen.height;
        float correctedRadius = holeRadius * aspectRatio;
        
        worldRadius = WorldRadiusFromNormalizedRadius(correctedRadius);
        circleCollider.radius = worldRadius;
    }

    float WorldRadiusFromNormalizedRadius(float normalizedRadius)
    {
        if (mainCamera == null) return 0f;

        if (mainCamera.orthographic)
        {
            float worldHeight = mainCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * Screen.width / Screen.height;
            float minDimension = Mathf.Min(worldWidth, worldHeight);
            return normalizedRadius * minDimension * 0.5f;
        }

        float worldZ = -mainCamera.transform.position.z;
        float screenDimension = Mathf.Min(Screen.width, Screen.height);
        Vector3 screenOrigin = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, worldZ);
        Vector3 screenRadius = new Vector3(Screen.width * 0.5f + screenDimension * normalizedRadius, Screen.height * 0.5f, worldZ);
        Vector3 worldOrigin = mainCamera.ScreenToWorldPoint(screenOrigin);
        Vector3 worldRadiusPoint = mainCamera.ScreenToWorldPoint(screenRadius);
        return Vector3.Distance(worldOrigin, worldRadiusPoint);
    }

    void DetectOverlappingObjects()
    {
        if (circleCollider == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, circleCollider.radius, detectionMask);
        HashSet<Collider2D> currentOverlaps = new HashSet<Collider2D>(hits);

        foreach (Collider2D hit in currentOverlaps)
        {
            if (hit == null) continue;
            overlappingColliders.Add(hit);
        }

        List<Collider2D> exited = new List<Collider2D>();
        foreach (Collider2D existing in overlappingColliders)
        {
            if (existing == null || !currentOverlaps.Contains(existing))
                exited.Add(existing);
        }

        foreach (Collider2D exit in exited)
        {
            overlappingColliders.Remove(exit);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((detectionMask.value & (1 << other.gameObject.layer)) == 0) return;

        if (overlappingColliders.Add(other))
            Debug.Log($"Entered flashlight hole: {other.gameObject.name} (count={insideCount})");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if ((detectionMask.value & (1 << other.gameObject.layer)) == 0) return;

        if (overlappingColliders.Remove(other))
            Debug.Log($"Exited flashlight hole: {other.gameObject.name} (count={insideCount})");
    }

    void OnGUI()
    {
        string mode = useEyeGaze ? "Eye Gaze" : "Mouse";
        GUI.Label(new Rect(10, 10, 300, 25), $"Flashlight input: {mode}  [{toggleKey} to toggle]");
    }

    void OnDrawGizmosSelected()
    {
        if (circleCollider == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
    }
}
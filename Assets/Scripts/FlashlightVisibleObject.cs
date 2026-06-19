using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FlashlightVisibleObject : MonoBehaviour
{
    private Enemy enemy;
    private bool isInLight = false;
    private FlashlightFollow flashlight;


    void Awake()
    {
        enemy = GetComponent<Enemy>();
        flashlight = FindObjectOfType<FlashlightFollow>();
    }


    void Reset()
    {
        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
            collider2D.isTrigger = false;
    }

    void Update()
    {
        if (enemy == null || flashlight == null) return;

        float distance = Vector2.Distance(transform.position, flashlight.transform.position);
        float radius = flashlight.GetWorldRadius() * 10.0f;

        // Debug.Log($"Enemy pos: {transform.position}, Flashlight pos: {flashlight.transform.position}, Distance: {distance}, WorldRadius: {radius}, InLight: {distance <= radius}");

        bool nowInLight = distance <= radius;

        if (nowInLight && !isInLight)
            Debug.Log($"{name} is inside the flashlight view.");
        else if (!nowInLight && isInLight)
        {
            enemy.OnOutsideFlashlight();
            Debug.Log($"{name} left the flashlight view.");
        }

        isInLight = nowInLight;

        if (isInLight)
            enemy.OnInsideFlashlight();
    }
}
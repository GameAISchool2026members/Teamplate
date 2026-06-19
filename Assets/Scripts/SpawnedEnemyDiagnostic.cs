using UnityEngine;

public class SpawnedEnemyDiagnostic : MonoBehaviour
{
    void Start()
    {
        DiagnoseEnemy();
    }

    public void DiagnoseEnemy()
    {
        Debug.Log($"=== Diagnosing Enemy: {gameObject.name} ===");

        // Check SpriteRenderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("❌ NO SpriteRenderer component!");
        }
        else
        {
            Debug.Log($"✓ SpriteRenderer found");
            Debug.Log($"  - Enabled: {spriteRenderer.enabled}");
            Debug.Log($"  - Sprite: {(spriteRenderer.sprite ? spriteRenderer.sprite.name : "NULL")}");
            Debug.Log($"  - Color: {spriteRenderer.color}");
            Debug.Log($"  - Sorting Order: {spriteRenderer.sortingOrder}");
            Debug.Log($"  - Sorting Layer: {spriteRenderer.sortingLayerName}");
        }

        // Check position
        Debug.Log($"✓ Position: {transform.position}");
        Debug.Log($"✓ Scale: {transform.localScale}");
        Debug.Log($"✓ Active: {gameObject.activeInHierarchy}");

        // Check other components
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            Debug.Log($"✓ Rigidbody2D: Body Type = {rb.bodyType}, Gravity = {rb.gravityScale}");

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            Debug.Log($"✓ Collider2D: Type = {collider.GetType().Name}, Enabled = {collider.enabled}");

        // Check the scene camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            float distance = Vector3.Distance(mainCam.transform.position, transform.position);
            Debug.Log($"✓ Distance from Main Camera: {distance}");
            Debug.Log($"  Camera position: {mainCam.transform.position}");
            Debug.Log($"  Camera far clip: {mainCam.farClipPlane}");
        }
    }
}

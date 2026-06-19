using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(FlashlightVisibleObject))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemySetupValidator : MonoBehaviour
{
    void OnEnable()
    {
        ValidateSetup();
    }

    public void ValidateSetup()
    {
        bool hasIssues = false;

        // Check Enemy component
        if (GetComponent<Enemy>() == null)
        {
            gameObject.AddComponent<Enemy>();
            hasIssues = true;
        }

        // Check EnemyMovement component
        if (GetComponent<EnemyMovement>() == null)
        {
            gameObject.AddComponent<EnemyMovement>();
            hasIssues = true;
        }

        // Check FlashlightVisibleObject component
        if (GetComponent<FlashlightVisibleObject>() == null)
        {
            gameObject.AddComponent<FlashlightVisibleObject>();
            hasIssues = true;
        }

        // Check Rigidbody2D
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            hasIssues = true;
        }

        if (rb != null)
        {
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                hasIssues = true;
            }
            if (rb.gravityScale != 0f)
            {
                rb.gravityScale = 0f;
                hasIssues = true;
            }
            if (rb.constraints != RigidbodyConstraints2D.FreezeRotation)
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                hasIssues = true;
            }
        }

        // Check Collider2D
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            gameObject.AddComponent<CircleCollider2D>();
            hasIssues = true;
        }

        // Check SpriteRenderer
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            hasIssues = true;
        }

        // Ensure sprite is assigned or load a default
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            // Try to find a goblin sprite as default
            Sprite[] allSprites = Resources.LoadAll<Sprite>("Assets/Sprites/Enemies");
            if (allSprites.Length > 0)
            {
                spriteRenderer.sprite = allSprites[0];
                hasIssues = true;
            }
        }

        if (hasIssues)
        {
            Debug.Log($"✓ Fixed enemy '{gameObject.name}' - added missing components and configured Rigidbody2D");
        }
        else
        {
            Debug.Log($"✓ Enemy '{gameObject.name}' is properly configured!");
        }
    }
}

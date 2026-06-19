using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab of the enemy to spawn.")]
    public GameObject enemyPrefab;

    [Tooltip("Radius around the spawner where enemies will spawn.")]
    public float spawnRadius = 5f;

    [Tooltip("Time in seconds between each spawn attempt.")]
    public float spawnCooldown = 2f;

    [Tooltip("Maximum number of alive enemies this spawner can maintain.")]
    public int maxEnemies = 5;

    [Tooltip("Delay before the first spawn (in seconds).")]
    public float startDelay = 0f;

    [Tooltip("Z position for spawned enemies (useful for 2D games).")]
    public float spawnZPosition = 0f;

    [Tooltip("Enable debug visualization of spawn radius.")]
    public bool showDebugRadius = true;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private float spawnTimer = 0f;
    private float startDelayTimer = 0f;
    private bool startDelayFinished = false;

    void Start()
    {
        startDelayTimer = startDelay;
        startDelayFinished = startDelay <= 0f;
    }

    void Update()
    {
        // Handle start delay
        if (!startDelayFinished)
        {
            startDelayTimer -= Time.deltaTime;
            if (startDelayTimer <= 0f)
            {
                startDelayFinished = true;
                spawnTimer = spawnCooldown;
            }
            return;
        }

        // Count down spawn timer
        spawnTimer -= Time.deltaTime;

        // Clean up dead enemies from the list
        CleanupDeadEnemies();

        // Spawn new enemy if cooldown is finished and we haven't hit the limit
        if (spawnTimer <= 0f && activeEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
            spawnTimer = spawnCooldown;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"EnemySpawner on {gameObject.name}: Enemy prefab is not assigned!");
            return;
        }

        // Calculate random spawn position within the radius
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, spawnZPosition);

        // Instantiate the enemy
        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        activeEnemies.Add(spawnedEnemy);

        Debug.Log($"Spawned enemy #{activeEnemies.Count} at position {spawnPosition}. Active enemies: {activeEnemies.Count}/{maxEnemies}");
    }

    private void CleanupDeadEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }

    public int GetActiveEnemyCount()
    {
        CleanupDeadEnemies();
        return activeEnemies.Count;
    }

    public List<GameObject> GetActiveEnemies()
    {
        CleanupDeadEnemies();
        return new List<GameObject>(activeEnemies);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugRadius)
            return;

        // Draw spawn radius as a green circle
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(transform.position, spawnRadius, 32);

        // Draw center point
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + new Vector3(radius, 0, 0);

        for (int i = 0; i <= segments; i++)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
            angle += angleStep;
        }
    }
}

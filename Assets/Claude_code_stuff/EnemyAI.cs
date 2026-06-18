using UnityEngine;

/// <summary>
/// The enemy chases the player. If it stays inside the vision circle long
/// enough, it dies. If it touches the player, game over.
/// Steering is simple (not real pathfinding): if the direct direction
/// toward the player is blocked by a wall, it tries alternate directions.
/// Good enough for a maze of demo size.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public VisionController vision;
    public LayerMask wallLayer;

    [Header("Movement")]
    public float speed = 2.1f; // ~60% of default player speed
    public float rayDistance = 0.6f;

    [Header("Kill via light exposure")]
    public float killExposureTime = 1.5f;
    public float exposureDecayRate = 2f; // how fast the timer drops when out of the light
    private float _exposureTimer = 0f;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Update()
    {
        UpdateExposure();
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        Vector2 toPlayer = ((Vector2)player.position - _rb.position).normalized;
        Vector2 moveDir = GetClearDirection(toPlayer);

        _rb.MovePosition(_rb.position + moveDir * speed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Tries the direct direction toward the player; if blocked by a wall,
    /// tries increasing alternate angles until it finds a clear one.
    /// </summary>
    private Vector2 GetClearDirection(Vector2 desiredDir)
    {
        float[] anglesToTry = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f };

        foreach (float angle in anglesToTry)
        {
            Vector2 dir = Quaternion.Euler(0, 0, angle) * desiredDir;
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, dir, rayDistance, wallLayer);
            if (hit.collider == null)
                return dir;
        }

        return Vector2.zero; // fully blocked, stay still this frame
    }

    private void UpdateExposure()
    {
        if (vision == null) return;

        float distToVision = Vector2.Distance(transform.position, vision.WorldPosition);
        bool isLit = distToVision <= vision.Radius;

        if (isLit)
        {
            _exposureTimer += Time.deltaTime;
            if (_exposureTimer >= killExposureTime)
                Die();
        }
        else
        {
            _exposureTimer = Mathf.Max(0f, _exposureTimer - Time.deltaTime * exposureDecayRate);
        }
    }

    private void Die()
    {
        // TODO: add death VFX/SFX here
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            GameManager.Instance?.TriggerLose();
        }
    }
}

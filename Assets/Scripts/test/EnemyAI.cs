using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform        player;
    public VisionController vision;
    public LayerMask        wallLayer;

    [Header("Movement")]
    public float speed       = 2.1f;
    public float rayDistance = 0.6f;

    [Header("Kill via light exposure")]
    public float killExposureTime  = 1.5f;
    public float exposureDecayRate = 2f;

    private float       _exposureTimer;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale  = 0f;
        _rb.freezeRotation = true;
    }

    private void Update()    => UpdateExposure();
    private void FixedUpdate()
    {
        if (player == null) return;
        Vector2 toPlayer = ((Vector2)player.position - _rb.position).normalized;
        _rb.MovePosition(_rb.position + GetClearDirection(toPlayer) * speed * Time.fixedDeltaTime);
    }

    // ------------------------------------------------------------------

    private Vector2 GetClearDirection(Vector2 desired)
    {
        float[] angles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f };
        foreach (float a in angles)
        {
            Vector2 dir = Quaternion.Euler(0, 0, a) * desired;
            if (!Physics2D.Raycast(_rb.position, dir, rayDistance, wallLayer))
                return dir;
        }
        return Vector2.zero;
    }

    private void UpdateExposure()
    {
        if (vision == null) return;
        bool isLit = Vector2.Distance(transform.position, vision.WorldPosition) <= vision.Radius;

        if (isLit)
        {
            _exposureTimer += Time.deltaTime;
            if (_exposureTimer >= killExposureTime) Die();
        }
        else
        {
            _exposureTimer = Mathf.Max(0f, _exposureTimer - Time.deltaTime * exposureDecayRate);
        }
    }

    private void Die()
    {
        GameManager.Instance?.NotifyEnemyKilled();
        // TODO: add death VFX/SFX here
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Player"))
            GameManager.Instance?.TriggerLose(LoseCause.EnemyContact);
    }
}

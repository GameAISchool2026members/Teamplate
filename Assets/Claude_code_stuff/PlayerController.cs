using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3.5f;

    private Rigidbody2D _rb;
    private Vector2 _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
    }

    private void Update()
    {
        _input.x = Input.GetAxisRaw("Horizontal"); // A/D
        _input.y = Input.GetAxisRaw("Vertical");   // W/S
        _input = _input.normalized;
    }

    private void FixedUpdate()
    {
        _rb.MovePosition(_rb.position + _input * moveSpeed * Time.fixedDeltaTime);
    }
}

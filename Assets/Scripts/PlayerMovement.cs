using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            moveInput = Vector2.zero;

            if (Keyboard.current.wKey.isPressed)
                moveInput.y += 1;

            if (Keyboard.current.sKey.isPressed)
                moveInput.y -= 1;

            if (Keyboard.current.aKey.isPressed)
                moveInput.x -= 1;

            if (Keyboard.current.dKey.isPressed)
                moveInput.x += 1;
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }
}
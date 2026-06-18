using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;

    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Automatically grabs the Animator if it's on this object
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleInput();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        // Move the player using physics
        // Note: Using .velocity (or .linearVelocity in Unity 6+)
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }

    private void HandleInput()
    {
        moveInput = Vector2.zero;

        // Using your exact New Input System logic, just tucked away neatly!
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
        }
    }

    private void UpdateAnimations()
    {
        // Safety check in case Animator isn't assigned
        if (animator == null) return; 

        // If we are pressing keys
        if (moveInput != Vector2.zero)
        {
            animator.SetBool("isMoving", true);

            // We ONLY update MoveX and MoveY when moving.
            // This way, when you let go of the keys, the Animator remembers 
            // the last direction you were facing and plays the correct Idle!
            animator.SetFloat("moveX", moveInput.x);
            animator.SetFloat("moveY", moveInput.y);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }
}
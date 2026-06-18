using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float moveSpeed = 2f;

    private Transform player;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void FixedUpdate()
    {
        if (player == null)
            return;

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player caught!");
        }
    }
    
}
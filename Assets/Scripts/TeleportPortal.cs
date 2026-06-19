using System.Collections;
using UnityEngine;

public class TeleportPortal : MonoBehaviour
{
    public Transform targetPortal;
    public float cooldown = 0.5f;

    private bool canTeleport = true;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canTeleport)
            return;

        if (!other.CompareTag("Player"))
            return;

        StartCoroutine(TeleportPlayer(other.transform));
    }

    private IEnumerator TeleportPlayer(Transform player)
    {
        canTeleport = false;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        // Teleport sesi
        if (audioSource != null && audioSource.clip != null)
        {
            AudioSource.PlayClipAtPoint(
                audioSource.clip,
                transform.position
            );
        }

        // Teleport işlemi
        player.position = targetPortal.position;

        TeleportPortal target = targetPortal.GetComponent<TeleportPortal>();

        if (target != null)
            target.StartCooldown();

        yield return new WaitForSeconds(cooldown);

        canTeleport = true;
    }

    public void StartCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        canTeleport = false;

        yield return new WaitForSeconds(cooldown);

        canTeleport = true;
    }
}
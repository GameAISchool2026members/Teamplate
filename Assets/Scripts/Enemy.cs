using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Flashlight Kill")]
    public float timeToKill = 2f;

    [Header("Audio")]
    public AudioClip startDyingClip;
    public AudioClip deathClip;
    public AudioSource audioSource; // optional: assign in inspector, used for one-shot death sounds
    public AudioSource startAudioSource; // optional: assign in inspector, used to play/stop start-dying cue

    [Header("Visual")]
    public SpriteRenderer spriteRenderer; // optional: assign in inspector
    public Color flashColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    private float timeInLight = 0f;
    private bool isDying = false;
    private bool hasPlayedStartCue = false;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Ensure there is an AudioSource if clips are assigned
        if (audioSource == null && deathClip != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Create a dedicated AudioSource for the start-dying cue so it can be stopped
        if (startAudioSource == null && startDyingClip != null)
        {
            // Try to find an existing one first
            AudioSource found = GetComponent<AudioSource>();
            if (found != null && (deathClip == null || audioSource != found))
            {
                // reuse if it's not the main death audio source
                startAudioSource = found;
            }
            else
            {
                startAudioSource = gameObject.AddComponent<AudioSource>();
            }

            startAudioSource.playOnAwake = false;
        }

        // Auto-find SpriteRenderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void OnInsideFlashlight()
    {
        if (isDying) return;

        // Play start-dying cue once when the enemy first enters the light
        if (!hasPlayedStartCue)
        {
            hasPlayedStartCue = true;
            if (startAudioSource != null && startDyingClip != null)
            {
                startAudioSource.clip = startDyingClip;
                startAudioSource.Play();
            }
            else if (audioSource != null && startDyingClip != null)
            {
                // fallback to one-shot if dedicated source isn't available
                audioSource.PlayOneShot(startDyingClip);
            }
        }

        timeInLight += Time.deltaTime;

        // Update visual tint based on exposure progress
        if (spriteRenderer != null)
        {
            float t = Mathf.Clamp01(timeInLight / timeToKill);
            spriteRenderer.color = Color.Lerp(Color.white, flashColor, t);
        }

        if (timeInLight >= timeToKill)
            StartCoroutine(Die());
    }

    public void OnOutsideFlashlight()
    {
        timeInLight = 0f;
        hasPlayedStartCue = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
        // Stop the start-dying cue if it's playing and the enemy escaped
        if (startAudioSource != null && startAudioSource.isPlaying && !isDying)
            startAudioSource.Stop();
    }

    IEnumerator Die()
    {
        isDying = true;
        Debug.Log("I am dying");

        if (animator != null)
        {
            animator.SetTrigger("Die");
            // If a death clip exists, play it and wait for the longer of animation or clip
            float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
            float clipLength = 0f;
            if (startAudioSource != null && startAudioSource.isPlaying)
                startAudioSource.Stop();

            if (audioSource != null && deathClip != null)
            {
                audioSource.PlayOneShot(deathClip);
                clipLength = deathClip.length;
            }

            float waitTime = Mathf.Max(animLength, clipLength);
            // Wait for the death animation or clip (whichever is longer)
            yield return new WaitForSeconds(waitTime);
        }
        else
        {
            // No animator: still play death clip if present
            if (audioSource != null && deathClip != null)
            {
                audioSource.PlayOneShot(deathClip);
                yield return new WaitForSeconds(deathClip.length);
            }
        }

        Destroy(gameObject);
    }
}
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Flashlight Kill")]
    public float timeToKill = 2f;

    private float timeInLight = 0f;
    private bool isDying = false;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void OnInsideFlashlight()
    {
        if (isDying) return;

        timeInLight += Time.deltaTime;

        if (timeInLight >= timeToKill)
            StartCoroutine(Die());
    }

    public void OnOutsideFlashlight()
    {
        timeInLight = 0f;
    }

    IEnumerator Die()
    {
        isDying = true;
        Debug.Log("I am dying");

        if (animator != null)
        {
            animator.SetTrigger("Die");
            // Wait for the death animation to finish
            yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        }

        Destroy(gameObject);
    }
}
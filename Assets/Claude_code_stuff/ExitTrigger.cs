using UnityEngine;

/// <summary>
/// Put this on a GameObject with a Collider2D set as a Trigger, positioned
/// on the "E" cell of the maze (see GDD section 5).
/// </summary>
public class ExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance?.TriggerWin();
        }
    }
}

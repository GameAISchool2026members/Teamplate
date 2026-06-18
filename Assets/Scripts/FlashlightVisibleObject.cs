using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FlashlightVisibleObject : MonoBehaviour
{
    void Reset()
    {
        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
            collider2D.isTrigger = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<FlashlightFollow>(out _))
        {
            Debug.Log($"{name} is inside the flashlight view.");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<FlashlightFollow>(out _))
        {
            Debug.Log($"{name} left the flashlight view.");
        }
    }
}

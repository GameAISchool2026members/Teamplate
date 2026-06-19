using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ensures the BlackOverlay canvas uses the main camera and a low sort order
/// so world sprites render above the overlay while the shader still darkens
/// the screen. Attach this to the BlackOverlay GameObject (prefab will get it at runtime).
/// </summary>
[ExecuteAlways]
public class BlackOverlayRuntimeFix : MonoBehaviour
{
    [Tooltip("Sorting order to assign to the overlay canvas (lower renders behind).")]
    public int runtimeSortingOrder = -100;

    void Awake()
    {
        ApplyFix();
    }

    void OnValidate()
    {
        ApplyFix();
    }

    void ApplyFix()
    {
        Canvas c = GetComponent<Canvas>();
        if (c == null)
            c = GetComponentInChildren<Canvas>();
        if (c == null)
            return;

        Camera main = Camera.main;
        if (main != null)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = main;
        }

        c.overrideSorting = true;
        c.sortingOrder = runtimeSortingOrder;
    }
}

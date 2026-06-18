using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightFollow : MonoBehaviour
{
    public Material flashlightMaterial;

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();

        Vector2 uv = new Vector2(
            mousePos.x / Screen.width,
            mousePos.y / Screen.height
        );

        flashlightMaterial.SetVector("_HoleCenter", uv);

        Debug.Log("Flashlight center (UV): " + uv);
    }
}
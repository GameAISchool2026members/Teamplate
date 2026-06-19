using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitDoor : MonoBehaviour
{
    private bool hasWon = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon) return;
        if (!other.CompareTag("Player")) return;

        hasWon = true;
        LoadWinScene();
    }

    void LoadWinScene()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerWin();
            return;
        }

        string winScene = string.IsNullOrEmpty(SceneNames.Win) ? "Win" : SceneNames.Win;
        SceneManager.LoadScene(winScene);
    }
}
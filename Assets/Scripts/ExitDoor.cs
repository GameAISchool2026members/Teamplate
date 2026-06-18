using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExitDoor : MonoBehaviour
{
    [Header("Win UI")]
    public GameObject winPanel;
    public TextMeshProUGUI winText;
    public string winMessage = "You escaped!";

    private bool hasWon = false;

    void Start()
    {
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon) return;
        if (!other.CompareTag("Player")) return;

        hasWon = true;
        ShowWinScreen();
    }

    void ShowWinScreen()
    {
        if (winPanel != null)
            winPanel.SetActive(true);

        if (winText != null)
            winText.text = winMessage;

        Time.timeScale = 0f;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
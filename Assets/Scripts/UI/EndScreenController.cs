using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla final al terminar todas las rondas.
public class EndScreenController : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject panel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI roundsText;
    public Button playAgainButton;

    private void Start()
    {
        GameManager.Instance.OnGameEnd += Show;

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameEnd -= Show;
    }

    private void Show()
    {
        if (panel != null) panel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = "Puntaje final: " + GameManager.Instance.Score;

        if (roundsText != null)
            roundsText.text = "Rondas jugadas: " + GameManager.Instance.totalRounds;
    }

    private void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void PlayAgain()
    {
        // StartGame reinicia el puntaje y tambien borra lo aprendido.
        Hide();
        GameManager.Instance.StartGame();
    }
}

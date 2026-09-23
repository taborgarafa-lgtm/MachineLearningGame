using TMPro;
using UnityEngine;

// Informacion en pantalla durante la partida.
// El puntaje se actualiza por evento porque cambia poco; el tiempo en Update
// porque cambia cada frame.
public class HUDController : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI scoreText;

    private void Start()
    {
        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnRoundStart += UpdateRound;

        UpdateScore();
        UpdateRound();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnScoreChanged -= UpdateScore;
        GameManager.Instance.OnRoundStart -= UpdateRound;
    }

    private void Update()
    {
        if (timerText == null || GameManager.Instance == null) return;

        // Ceil para que muestre 10 al empezar y 1 durante el ultimo segundo.
        int seconds = Mathf.CeilToInt(GameManager.Instance.TimeLeft);
        timerText.text = seconds + "s";
    }

    private void UpdateScore()
    {
        if (scoreText == null) return;
        scoreText.text = "Puntaje: " + GameManager.Instance.Score;
    }

    private void UpdateRound()
    {
        if (roundText == null) return;
        roundText.text = "Ronda " + GameManager.Instance.CurrentRound
                       + " / " + GameManager.Instance.totalRounds;
    }
}

using TMPro;
using UnityEngine;

/// <summary>
/// La informacion que se ve durante la partida: tiempo, ronda y puntaje.
///
/// El puntaje se actualiza por evento (solo cuando cambia) y el tiempo en
/// Update (porque cambia cada frame). No tiene sentido hacer las dos cosas
/// de la misma manera.
/// </summary>
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

        // Ceil para que muestre "10" el primer instante y "1" el ultimo
        // segundo completo, en vez de bajar a 0 antes de tiempo.
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

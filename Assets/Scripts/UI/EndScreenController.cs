using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// La pantalla que aparece al terminar las 15 rondas.
///
/// El boton de volver a jugar llama a GameManager.StartGame(), que reinicia
/// el puntaje Y TAMBIEN borra todo lo aprendido. Eso es a proposito: una
/// partida nueva tiene que empezar desde cero, o el sistema arrancaria ya
/// sabiendo la respuesta y no se veria nada aprender.
/// </summary>
public class EndScreenController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El panel entero de la pantalla final.")]
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
        Hide();
        GameManager.Instance.StartGame();
    }
}

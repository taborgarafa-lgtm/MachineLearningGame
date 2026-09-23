using System;
using System.Collections;
using UnityEngine;

// Lleva las rondas, el tiempo y el puntaje. No sabe nada de celulas ni de
// aprendizaje: solo avisa cuando una ronda empieza y termina, y los demas
// scripts reaccionan a esos avisos.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuracion de la partida")]
    [Tooltip("Segundos que dura cada ronda.")]
    public float roundDuration = 10f;

    [Tooltip("Rondas que tiene una partida completa.")]
    public int totalRounds = 15;

    [Header("Referencias")]
    [Tooltip("Objeto que tiene el componente de aprendizaje.")]
    public LearningManagerBase learning;

    public int CurrentRound { get; private set; }
    public int Score { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsPlaying { get; private set; }

    public event Action OnRoundStart;
    public event Action OnRoundEnd;
    public event Action OnGameEnd;
    public event Action OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (learning == null)
        {
            Debug.LogError("GameManager: falta asignar el componente de aprendizaje.");
            return;
        }
        StartGame();
    }

    // Tambien la llama el boton de volver a jugar.
    public void StartGame()
    {
        StopAllCoroutines();

        Score = 0;
        CurrentRound = 0;
        TimeLeft = 0f;

        // Una partida nueva empieza sin nada aprendido, o no se veria aprender.
        learning.ResetLearning();

        OnScoreChanged?.Invoke();
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        IsPlaying = true;

        // Esperamos un frame para que todos los scripts hayan corrido su Start()
        // y se hayan suscrito. Sin esto el spawner se pierde la ronda 1.
        yield return null;

        while (CurrentRound < totalRounds)
        {
            CurrentRound++;
            OnRoundStart?.Invoke();

            TimeLeft = roundDuration;
            while (TimeLeft > 0f)
            {
                TimeLeft -= Time.deltaTime;
                yield return null;
            }
            TimeLeft = 0f;

            OnRoundEnd?.Invoke();

            // Margen de un frame para que todos procesen el fin de ronda.
            yield return null;
        }

        IsPlaying = false;
        Debug.Log("Partida terminada. Puntaje final: " + Score);
        OnGameEnd?.Invoke();
    }

    public void AddScore(int points)
    {
        Score += points;
        OnScoreChanged?.Invoke();
    }
}

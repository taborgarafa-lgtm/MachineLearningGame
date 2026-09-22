using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// El cerebro de la partida: lleva las rondas, el tiempo y el puntaje.
///
/// No sabe nada de celulas ni de aprendizaje. Solo avisa cuando una ronda
/// empieza y cuando termina, y los demas scripts reaccionan a esos avisos.
/// Eso se llama desacoplar, y hace que cada script se pueda probar solo.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Singleton: permite que cualquier script llegue al GameManager
    /// escribiendo GameManager.Instance, sin tener que arrastrarlo
    /// en el Inspector a cada objeto.
    /// </summary>
    public static GameManager Instance { get; private set; }

    [Header("Configuracion de la partida")]
    [Tooltip("Cuantos segundos dura cada ronda.")]
    public float roundDuration = 10f;

    [Tooltip("Cuantas rondas tiene una partida completa.")]
    public int totalRounds = 15;

    [Header("Referencias")]
    [Tooltip("Arrastra aqui el objeto que tiene el componente de aprendizaje. " +
             "Durante el Dia 1 sera DummyLearning; el Dia 2 se cambia por el real.")]
    public LearningManagerBase learning;

    // --- Estado de la partida (solo lectura desde fuera) ---
    public int CurrentRound { get; private set; }
    public int Score { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsPlaying { get; private set; }

    // --- Avisos a los que otros scripts se pueden suscribir ---

    /// <summary>Se dispara al empezar cada ronda. Lo escucha el spawner para crear celulas.</summary>
    public event Action OnRoundStart;

    /// <summary>
    /// Se dispara al terminar cada ronda. Lo escucha el spawner, que primero
    /// reporta el resultado de cada celula al sistema de aprendizaje y despues
    /// las retira de pantalla. El orden importa: primero reportar, luego borrar.
    /// </summary>
    public event Action OnRoundEnd;

    /// <summary>Se dispara al terminar la ultima ronda. Lo escucha la UI final.</summary>
    public event Action OnGameEnd;

    /// <summary>Se dispara cada vez que cambia el puntaje. Lo escucha el HUD.</summary>
    public event Action OnScoreChanged;

    private void Awake()
    {
        // Si ya existe otro GameManager en la escena, este sobra.
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
            Debug.LogError("GameManager: falta asignar el componente de aprendizaje en el Inspector.");
            return;
        }
        StartGame();
    }

    /// <summary>
    /// Arranca una partida desde cero. Tambien la llama el boton
    /// de "volver a jugar" de la pantalla final.
    /// </summary>
    public void StartGame()
    {
        StopAllCoroutines();

        Score = 0;
        CurrentRound = 0;
        TimeLeft = 0f;

        // Importante: una partida nueva empieza sin nada aprendido.
        learning.ResetLearning();

        OnScoreChanged?.Invoke();
        StartCoroutine(GameLoop());
    }

    /// <summary>
    /// El ciclo principal. Usamos una corrutina en lugar de contar en Update
    /// porque el flujo se lee de arriba abajo como una receta, y porque
    /// evita comprobar condiciones en cada frame durante toda la partida.
    /// </summary>
    private IEnumerator GameLoop()
    {
        IsPlaying = true;

        // Esperamos un frame antes de la primera ronda para que todos los
        // scripts hayan corrido su Start() y se hayan suscrito a los eventos.
        // Sin esto, el spawner podria perderse el aviso de la ronda 1.
        yield return null;

        while (CurrentRound < totalRounds)
        {
            CurrentRound++;
            Debug.Log("=== Ronda " + CurrentRound + " de " + totalRounds + " ===");
            OnRoundStart?.Invoke();

            // Cuenta regresiva de la ronda.
            TimeLeft = roundDuration;
            while (TimeLeft > 0f)
            {
                TimeLeft -= Time.deltaTime;
                yield return null; // esperar al siguiente frame
            }
            TimeLeft = 0f;

            OnRoundEnd?.Invoke();

            // Un frame de margen para que todos terminen de procesar
            // el fin de ronda antes de que empiece la siguiente.
            yield return null;
        }

        IsPlaying = false;
        Debug.Log("=== Partida terminada. Puntaje final: " + Score + " ===");
        OnGameEnd?.Invoke();
    }

    /// <summary>Suma puntos. La llama el sistema de input al eliminar una celula.</summary>
    public void AddScore(int points)
    {
        Score += points;
        OnScoreChanged?.Invoke();
    }
}

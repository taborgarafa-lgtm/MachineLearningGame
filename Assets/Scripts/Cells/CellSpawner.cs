using System.Collections.Generic;
using UnityEngine;

// Crea las celulas al empezar cada ronda y las retira al terminarla.
// Es el puente entre el juego y el sistema de aprendizaje: le pide un genoma
// por celula y al cerrar la ronda le devuelve el resultado.
public class CellSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Solo se usa si Use Pooling esta desmarcado.")]
    public Cell cellPrefab;
    public CellPool pool;

    [Header("Configuracion")]
    public int cellsPerRound = 8;

    [Tooltip("Tope de seguridad: nunca habra mas celulas a la vez.")]
    public int maxCellsOnScreen = 20;

    [Tooltip("Margen desde el borde de la pantalla, en unidades de mundo.")]
    public float borderMargin = 1f;

    [Tooltip("Distancia minima entre dos celulas al nacer.")]
    public float minDistance = 1.2f;

    [Header("Optimizacion")]
    [Tooltip("Desmarcalo para comparar en el Profiler con y sin pooling.")]
    public bool usePooling = true;

    private readonly List<Cell> liveCells = new List<Cell>();
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        // En Start y no en Awake: GameManager.Instance se asigna en su Awake.
        GameManager.Instance.OnRoundStart += SpawnRound;
        GameManager.Instance.OnRoundEnd += FinishRound;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnRoundStart -= SpawnRound;
        GameManager.Instance.OnRoundEnd -= FinishRound;
    }

    private void SpawnRound()
    {
        LearningManagerBase learning = GameManager.Instance.learning;
        int amount = Mathf.Min(cellsPerRound, maxCellsOnScreen);

        for (int i = 0; i < amount; i++)
        {
            // El sistema de aprendizaje decide que probar esta vez.
            CellGenome genome = learning.SelectGenome();
            Color color = learning.GetColor(genome.colorIndex);
            float size = learning.GetSize(genome.sizeIndex);

            Vector3 position = FindFreePosition();

            Cell cell = (usePooling && pool != null)
                ? pool.Get(position)
                : Instantiate(cellPrefab, position, Quaternion.identity, transform);

            // El indice sirve de orden de dibujo: la ultima creada queda encima.
            cell.Initialize(genome, color, size, i);

            liveCells.Add(cell);
        }
    }

    private void FinishRound()
    {
        float duration = GameManager.Instance.roundDuration;
        LearningManagerBase learning = GameManager.Instance.learning;

        // EL ORDEN IMPORTA: si retiramos las celulas antes de reportar,
        // se pierde el dato y el sistema nunca aprende nada.

        // 1. Cada celula reporta que fraccion de la ronda aguanto viva.
        foreach (Cell cell in liveCells)
        {
            if (cell == null) continue;
            learning.RegisterResult(cell.Genome, cell.GetSurvivalRatio(duration));
        }

        // 2. El sistema cierra la ronda: decae epsilon y guarda estadisticas.
        learning.EndRound();

        // 3. Recien ahora se retiran las celulas.
        foreach (Cell cell in liveCells)
        {
            if (cell == null) continue;

            if (usePooling && pool != null) pool.Return(cell);
            else Destroy(cell.gameObject);
        }
        liveCells.Clear();
    }

    private Vector3 FindFreePosition()
    {
        float halfHeight = cam.orthographicSize - borderMargin;
        float halfWidth = cam.orthographicSize * cam.aspect - borderMargin;

        Vector3 position = Vector3.zero;

        // Tras 20 intentos aceptamos la ultima posicion: es preferible
        // a quedarse en un bucle infinito si no hay hueco libre.
        for (int attempt = 0; attempt < 20; attempt++)
        {
            position = new Vector3(
                Random.Range(-halfWidth, halfWidth),
                Random.Range(-halfHeight, halfHeight),
                0f);

            if (IsFree(position)) break;
        }

        return position;
    }

    private bool IsFree(Vector3 position)
    {
        foreach (Cell cell in liveCells)
        {
            if (cell == null) continue;
            if (Vector3.Distance(cell.transform.position, position) < minDistance) return false;
        }
        return true;
    }
}

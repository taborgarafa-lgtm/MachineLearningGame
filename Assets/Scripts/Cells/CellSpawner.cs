using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea las celulas al empezar cada ronda y las retira al terminarla.
///
/// Es el puente entre el juego y el sistema de aprendizaje: le pide un
/// genoma por cada celula, y al cerrar la ronda le devuelve el resultado.
/// </summary>
public class CellSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aqui el prefab de la celula. " +
             "Solo se usa si Use Pooling esta desmarcado.")]
    public Cell cellPrefab;

    [Tooltip("Arrastra aqui el objeto que tiene el CellPool.")]
    public CellPool pool;

    [Header("Configuracion")]
    [Tooltip("Cuantas celulas nacen en cada ronda.")]
    public int cellsPerRound = 8;

    [Tooltip("Tope de seguridad: nunca habra mas de estas celulas a la vez.")]
    public int maxCellsOnScreen = 20;

    [Tooltip("Margen desde el borde de la pantalla, en unidades de mundo.")]
    public float borderMargin = 1f;

    [Tooltip("Distancia minima entre dos celulas para que no nazcan una encima de otra.")]
    public float minDistance = 1.2f;

    [Header("Optimizacion")]
    [Tooltip("Con pooling se reutilizan las celulas en vez de crearlas y destruirlas. " +
             "Desmarcalo para medir en el Profiler la diferencia entre ambos modos.")]
    public bool usePooling = true;

    // Las celulas vivas de la ronda actual.
    private readonly List<Cell> liveCells = new List<Cell>();

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Start()
    {
        // Nos suscribimos en Start y no en Awake porque GameManager.Instance
        // se asigna en su Awake: asi nos aseguramos de que ya exista.
        GameManager.Instance.OnRoundStart += SpawnRound;
        GameManager.Instance.OnRoundEnd += FinishRound;
    }

    private void OnDestroy()
    {
        // Siempre hay que desuscribirse, o quedan referencias colgando.
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnRoundStart -= SpawnRound;
        GameManager.Instance.OnRoundEnd -= FinishRound;
    }

    /// <summary>Crea la tanda de celulas de la ronda.</summary>
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

            // Con pooling pedimos una celula prestada; sin pooling creamos
            // una nueva cada vez. El resto del codigo es identico.
            Cell cell = (usePooling && pool != null)
                ? pool.Get(position)
                : Instantiate(cellPrefab, position, Quaternion.identity, transform);

            // El indice i sirve de orden de dibujo: la ultima creada queda encima.
            cell.Initialize(genome, color, size, i);

            liveCells.Add(cell);
        }
    }

    /// <summary>
    /// Cierra la ronda. EL ORDEN DE ESTOS TRES PASOS IMPORTA:
    /// si borramos las celulas antes de reportar, se pierde todo el dato
    /// y el sistema nunca aprende nada.
    /// </summary>
    private void FinishRound()
    {
        float duration = GameManager.Instance.roundDuration;
        LearningManagerBase learning = GameManager.Instance.learning;

        // 1. Cada celula reporta que fraccion de la ronda aguanto viva.
        foreach (Cell cell in liveCells)
        {
            if (cell == null) continue;
            learning.RegisterResult(cell.Genome, cell.GetSurvivalRatio(duration));
        }

        // 2. El sistema cierra la ronda (hace decaer epsilon, guarda estadisticas).
        learning.EndRound();

        // 3. Recien ahora retiramos las celulas de la pantalla.
        foreach (Cell cell in liveCells)
        {
            if (cell == null) continue;

            if (usePooling && pool != null) pool.Return(cell);
            else Destroy(cell.gameObject);
        }
        liveCells.Clear();
    }

    /// <summary>
    /// Busca un punto libre dentro del area visible de la camara.
    /// Lo intenta varias veces; si no encuentra hueco, acepta el ultimo punto.
    /// Es preferible a quedarse en un bucle infinito.
    /// </summary>
    private Vector3 FindFreePosition()
    {
        float halfHeight = cam.orthographicSize - borderMargin;
        float halfWidth = cam.orthographicSize * cam.aspect - borderMargin;

        Vector3 position = Vector3.zero;

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

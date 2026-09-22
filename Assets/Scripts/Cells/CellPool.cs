using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reserva de celulas reutilizables (object pooling).
///
/// EL PROBLEMA: Instantiate y Destroy son caros. Instantiate reserva memoria
/// nueva y Destroy la deja para que el recolector de basura la limpie despues,
/// lo que provoca tirones. Con 15 rondas por 8 celulas son 120 ciclos de
/// crear y destruir en una sola partida.
///
/// LA SOLUCION: crear las celulas UNA vez al arrancar y despues solo
/// activarlas y desactivarlas. Desactivar un objeto no reserva ni libera
/// memoria: es practicamente gratis.
/// </summary>
public class CellPool : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aqui el prefab de la celula.")]
    public Cell cellPrefab;

    [Header("Configuracion")]
    [Tooltip("Cuantas celulas se crean por adelantado al arrancar. " +
             "Ponlo igual o mayor que el maximo de celulas en pantalla.")]
    public int prewarmCount = 20;

    // Las celulas libres, esperando a ser usadas.
    private readonly Queue<Cell> available = new Queue<Cell>();

    // Cuantas se han creado en total (util para comprobar en el Profiler
    // que despues del arranque ya no se crea ninguna mas).
    public int TotalCreated { get; private set; }

    private void Awake()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            available.Enqueue(CreateNew());
        }
    }

    private Cell CreateNew()
    {
        Cell cell = Instantiate(cellPrefab, transform);
        cell.gameObject.SetActive(false);
        TotalCreated++;
        return cell;
    }

    /// <summary>
    /// Entrega una celula lista para usar. Si la reserva esta vacia crea una
    /// nueva, pero eso solo deberia pasar si prewarmCount se quedo corto.
    /// </summary>
    public Cell Get(Vector3 position)
    {
        Cell cell = available.Count > 0 ? available.Dequeue() : CreateNew();

        cell.transform.position = position;
        cell.gameObject.SetActive(true);

        return cell;
    }

    /// <summary>Devuelve una celula a la reserva en lugar de destruirla.</summary>
    public void Return(Cell cell)
    {
        if (cell == null) return;

        cell.gameObject.SetActive(false);
        available.Enqueue(cell);
    }
}

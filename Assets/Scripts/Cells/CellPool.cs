using System.Collections.Generic;
using UnityEngine;

// Reserva de celulas reutilizables (object pooling).
//
// Instantiate reserva memoria y Destroy deja trabajo al recolector de basura,
// lo que provoca tirones. Una partida son 120 ciclos de crear y destruir.
// Aqui las celulas se crean una vez y despues solo se activan y desactivan,
// que no reserva ni libera memoria.
public class CellPool : MonoBehaviour
{
    [Header("Referencias")]
    public Cell cellPrefab;

    [Header("Configuracion")]
    [Tooltip("Celulas creadas por adelantado. Igual o mayor al maximo en pantalla.")]
    public int prewarmCount = 20;

    private readonly Queue<Cell> available = new Queue<Cell>();

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

    public Cell Get(Vector3 position)
    {
        // Si la reserva esta vacia creamos una nueva, pero eso solo pasa
        // si prewarmCount se quedo corto.
        Cell cell = available.Count > 0 ? available.Dequeue() : CreateNew();

        cell.transform.position = position;
        cell.gameObject.SetActive(true);

        return cell;
    }

    public void Return(Cell cell)
    {
        if (cell == null) return;

        cell.gameObject.SetActive(false);
        available.Enqueue(cell);
    }
}

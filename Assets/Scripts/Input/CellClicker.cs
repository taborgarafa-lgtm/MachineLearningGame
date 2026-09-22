using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detecta los clics del jugador y elimina la celula que este debajo del cursor.
///
/// Hay UN SOLO script de input en toda la escena, en lugar de un OnMouseDown
/// por celula. La diferencia: con OnMouseDown, Unity hace una consulta de
/// fisica por cada objeto y en cada frame. Aqui se hace una sola consulta,
/// y unicamente en el frame en que hubo clic.
/// </summary>
public class CellClicker : MonoBehaviour
{
    [Header("Configuracion")]
    [Tooltip("Puntos que suma cada celula eliminada.")]
    public int pointsPerCell = 1;

    [Tooltip("Marca aqui solo la capa Celulas, para que la consulta ignore el resto.")]
    public LayerMask cellLayer;

    private Camera cam;

    // Reutilizamos siempre la misma lista y el mismo filtro.
    // La version OverlapPointAll devuelve un array nuevo en cada clic;
    // esta version escribe sobre la lista que ya tenemos y no reserva memoria.
    private readonly List<Collider2D> hits = new List<Collider2D>();
    private ContactFilter2D filter;

    private void Awake()
    {
        cam = Camera.main;

        filter = new ContactFilter2D();
        filter.SetLayerMask(cellLayer);
        filter.useTriggers = true;
    }

    private void Update()
    {
        // El proyecto usa el Input System nuevo, por eso Mouse.current
        // en lugar del viejo Input.GetMouseButtonDown.
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = cam.ScreenToWorldPoint(screenPosition);

        // Una sola consulta de fisica, limitada a la capa de celulas,
        // y sin reservar memoria nueva.
        int count = Physics2D.OverlapPoint(worldPosition, filter, hits);
        if (count == 0) return;

        // Si hay varias celulas solapadas, debe morir la que se ve ENCIMA,
        // que es la de mayor orden de dibujo.
        Cell topCell = null;
        int topOrder = int.MinValue;

        for (int i = 0; i < count; i++)
        {
            Cell cell = hits[i].GetComponent<Cell>();
            if (cell == null || !cell.IsAlive) continue;

            if (cell.DrawOrder > topOrder)
            {
                topCell = cell;
                topOrder = cell.DrawOrder;
            }
        }

        if (topCell == null) return;

        topCell.Kill();
        GameManager.Instance.AddScore(pointsPerCell);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Detecta los clics y elimina la celula que este debajo del cursor.
//
// Hay un solo script de input en toda la escena en lugar de un OnMouseDown por
// celula: asi se hace una unica consulta de fisica, y solo en el frame del clic.
public class CellClicker : MonoBehaviour
{
    [Header("Configuracion")]
    public int pointsPerCell = 1;

    [Tooltip("Marcar solo la capa Celulas.")]
    public LayerMask cellLayer;

    private Camera cam;

    // Lista y filtro reutilizados: esta version de OverlapPoint escribe sobre
    // ellos en vez de devolver un array nuevo en cada clic.
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
        // El proyecto usa el Input System nuevo: Mouse.current en lugar de Input.
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = cam.ScreenToWorldPoint(screenPosition);

        int count = Physics2D.OverlapPoint(worldPosition, filter, hits);
        if (count == 0) return;

        // Si hay varias solapadas debe morir la que se ve encima.
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

using UnityEngine;

/// <summary>
/// Una celula individual.
///
/// Su unica responsabilidad es: mostrarse con el color y el tamanio que le
/// tocaron, y llevar la cuenta de cuanto tiempo aguanto viva. No decide nada
/// sobre el aprendizaje: solo reporta el dato para que el LearningManager
/// saque sus conclusiones.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Cell : MonoBehaviour
{
    /// <summary>Que color y tamanio le toco a esta celula.</summary>
    public CellGenome Genome { get; private set; }

    /// <summary>False desde el momento en que el jugador le hace clic.</summary>
    public bool IsAlive { get; private set; }

    /// <summary>
    /// Orden de dibujo. Cuando dos celulas se solapan, la de mayor
    /// DrawOrder es la que se ve encima, y es la que debe morir al hacer clic.
    /// </summary>
    public int DrawOrder { get; private set; }

    // Guardamos la referencia una sola vez en Awake.
    // Llamar a GetComponent cada frame seria mucho mas lento.
    private SpriteRenderer spriteRenderer;

    private float spawnTime;
    private float deathTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Prepara la celula al nacer. La llama el CellSpawner con los valores
    /// que le dio el sistema de aprendizaje.
    /// </summary>
    public void Initialize(CellGenome genome, Color color, float size, int drawOrder)
    {
        Genome = genome;
        IsAlive = true;
        DrawOrder = drawOrder;

        spawnTime = Time.time;
        deathTime = -1f;

        spriteRenderer.color = color;
        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = drawOrder;
        transform.localScale = Vector3.one * size;
    }

    /// <summary>
    /// Marca la celula como eliminada y anota el instante exacto.
    /// No la destruye: la celula sigue existiendo hasta el fin de la ronda
    /// para poder reportar su resultado.
    /// </summary>
    public void Kill()
    {
        if (!IsAlive) return;

        IsAlive = false;
        deathTime = Time.time;

        // Se oculta, pero el objeto sigue vivo para reportar despues.
        spriteRenderer.enabled = false;
    }

    /// <summary>
    /// Que fraccion de la ronda logro sobrevivir, de 0 a 1.
    ///
    /// Esta es la RECOMPENSA del sistema de aprendizaje.
    /// Usamos tiempo en vez de un simple sobrevivio/no sobrevivio porque
    /// una celula que aguanta 9 de 10 segundos aporta mucha mas informacion
    /// que una que murio al primer segundo. Con solo 15 rondas de datos,
    /// esa diferencia hace que el sistema aprenda bastante mas rapido.
    /// </summary>
    public float GetSurvivalRatio(float roundDuration)
    {
        if (roundDuration <= 0f) return 0f;

        float livedTime = IsAlive
            ? (Time.time - spawnTime)     // nunca la mataron
            : (deathTime - spawnTime);    // la mataron en deathTime

        return Mathf.Clamp01(livedTime / roundDuration);
    }
}

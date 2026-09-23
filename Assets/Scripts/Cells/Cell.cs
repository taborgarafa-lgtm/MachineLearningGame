using UnityEngine;

// Una celula individual. Se muestra con el color y el tamanio que le tocaron
// y lleva la cuenta de cuanto aguanto viva. No decide nada del aprendizaje:
// solo aporta el dato.
public class Cell : MonoBehaviour
{
    public CellGenome Genome { get; private set; }
    public bool IsAlive { get; private set; }

    // Cuando dos celulas se solapan, la de mayor DrawOrder es la que se ve
    // encima y la que debe morir al hacer clic.
    public int DrawOrder { get; private set; }

    private SpriteRenderer spriteRenderer;
    private float spawnTime;
    private float deathTime;

    private void Awake()
    {
        // Cacheamos la referencia una sola vez.
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

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

    public void Kill()
    {
        if (!IsAlive) return;

        IsAlive = false;
        deathTime = Time.time;

        // Se oculta pero el objeto sigue vivo hasta el fin de ronda,
        // para poder reportar su resultado.
        spriteRenderer.enabled = false;
    }

    // Esta es la RECOMPENSA del sistema de aprendizaje. Usamos tiempo y no un
    // simple sobrevivio/no sobrevivio porque una celula que aguanta 9 de 10
    // segundos aporta mucha mas informacion que una que murio al primer segundo.
    public float GetSurvivalRatio(float roundDuration)
    {
        if (roundDuration <= 0f) return 0f;

        float livedTime = IsAlive
            ? Time.time - spawnTime
            : deathTime - spawnTime;

        return Mathf.Clamp01(livedTime / roundDuration);
    }
}

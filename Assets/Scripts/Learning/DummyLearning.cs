using UnityEngine;

/// <summary>
/// Implementacion PROVISIONAL del sistema de aprendizaje: elige todo al azar
/// y no aprende nada.
///
/// Existe para que el juego se pueda construir y probar entero sin esperar a
/// que el LearningManager real este listo. El Dia 2 se arrastra el componente
/// real al GameManager y este se puede borrar.
///
/// Los arrays de colores y tamanios que estan aqui son los definitivos:
/// los hereda el LearningManager real.
/// </summary>
public class DummyLearning : LearningManagerBase
{
    [Header("Las 8 opciones de color")]
    [Tooltip("El indice 4 es el turquesa, parecido al fondo. Es el color " +
             "hacia el que el sistema real deberia converger.")]
    public Color[] colors = new Color[]
    {
        new Color(0.85f, 0.35f, 0.35f), // 0 rojo
        new Color(0.85f, 0.60f, 0.30f), // 1 naranja
        new Color(0.85f, 0.85f, 0.35f), // 2 amarillo
        new Color(0.40f, 0.75f, 0.40f), // 3 verde
        new Color(0.30f, 0.70f, 0.65f), // 4 turquesa  <-- parecido al fondo
        new Color(0.35f, 0.50f, 0.85f), // 5 azul
        new Color(0.60f, 0.40f, 0.85f), // 6 violeta
        new Color(0.85f, 0.40f, 0.65f)  // 7 rosa
    };

    [Header("Las 4 opciones de tamanio")]
    [Tooltip("Limites del enunciado: minimo 0.5, maximo 1.25.")]
    public float[] sizes = new float[] { 0.5f, 0.75f, 1.0f, 1.25f };

    public override int ColorCount => colors.Length;
    public override int SizeCount => sizes.Length;

    /// <summary>Todo al azar: aqui no hay ningun aprendizaje todavia.</summary>
    public override CellGenome SelectGenome()
    {
        return new CellGenome(
            Random.Range(0, colors.Length),
            Random.Range(0, sizes.Length)
        );
    }

    /// <summary>No hace nada. El sistema real es el que usara este dato.</summary>
    public override void RegisterResult(CellGenome genome, float survivalRatio)
    {
        // Vacio a proposito.
    }

    public override Color GetColor(int colorIndex)
    {
        return colors[Mathf.Clamp(colorIndex, 0, colors.Length - 1)];
    }

    public override float GetSize(int sizeIndex)
    {
        return sizes[Mathf.Clamp(sizeIndex, 0, sizes.Length - 1)];
    }

    public override void ResetLearning()
    {
        // Vacio a proposito: no hay nada que reiniciar.
    }
}

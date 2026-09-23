using UnityEngine;

/// <summary>
/// EL SISTEMA DE APRENDIZAJE REAL.
///
/// Que es: un bandido multibrazo con politica epsilon-greedy.
/// Es aprendizaje por refuerzo: no hay datos etiquetados ni reglas escritas
/// a mano sobre que color es bueno. El sistema lo descubre solo, por prueba
/// y error, a partir de una senial de recompensa.
///
/// Las cuatro piezas del problema:
///   Agente     -> la poblacion de celulas
///   Accion     -> elegir un color y un tamanio
///   Entorno    -> el fondo de la escena + el jugador haciendo clic
///   Recompensa -> que fraccion de la ronda logro sobrevivir (0 a 1)
///
/// Por que DOS tablas separadas y no una combinada de 8x4 = 32 casillas:
/// con rondas de 10 segundos y 8 celulas por ronda tenemos unas 8 muestras
/// por ronda. Llenar 32 casillas necesitaria mas de 40 rondas; llenar
/// 8 + 4 converge en 10 o 15. Como la partida dura 15 rondas, la version
/// separada es la unica que alcanza a mostrar que aprende.
/// </summary>
public class LearningManager : LearningManagerBase
{
    [Header("Las 8 opciones de color")]
    [Tooltip("El indice 4 es turquesa (#4DB3A6), parecido al fondo (#338C85). " +
             "Es el color hacia el que el sistema deberia converger.")]
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

    [Header("Parametros del aprendizaje")]
    [Tooltip("Alfa: cuanto pesa cada resultado nuevo frente a lo ya aprendido. " +
             "Mas alto = aprende mas rapido pero es mas inestable.")]
    [Range(0.01f, 1f)] public float alpha = 0.2f;

    [Tooltip("Epsilon inicial: probabilidad de explorar en la ronda 1.")]
    [Range(0f, 1f)] public float startingEpsilon = 0.9f;

    [Tooltip("Epsilon nunca baja de aqui: siempre queda algo de exploracion.")]
    [Range(0f, 1f)] public float minimumEpsilon = 0.1f;

    [Tooltip("Cuanto se multiplica epsilon al cerrar cada ronda.")]
    [Range(0.5f, 1f)] public float decayFactor = 0.9f;

    [Tooltip("Si esta activo, parte de la exploracion prueba colores VECINOS " +
             "al mejor conocido en vez de saltar a cualquiera (busqueda local).")]
    public bool useLocalMutation = true;

    [Header("Opcional")]
    [Tooltip("Arrastra aqui el SurvivalLogger si quieres exportar el CSV.")]
    public SurvivalLogger logger;

    // --- Estado del aprendizaje (lo lee el panel de debug) ---

    /// <summary>Que tan bien ha funcionado cada color hasta ahora (0 a 1).</summary>
    public float[] QColor { get; private set; }

    /// <summary>Que tan bien ha funcionado cada tamanio hasta ahora (0 a 1).</summary>
    public float[] QSize { get; private set; }

    /// <summary>Probabilidad actual de explorar.</summary>
    public float Epsilon { get; private set; }

    // Estadisticas de la ronda en curso, para el CSV.
    private int roundSurvivors;
    private int roundTotal;

    public override int ColorCount => colors.Length;
    public override int SizeCount => sizes.Length;

    private void Awake()
    {
        ResetLearning();
    }

    // ------------------------------------------------------------------
    //  ELEGIR: politica epsilon-greedy
    // ------------------------------------------------------------------

    public override CellGenome SelectGenome()
    {
        return new CellGenome(ChooseIndex(QColor), ChooseIndex(QSize));
    }

    /// <summary>
    /// El dilema exploracion vs explotacion.
    ///
    ///   con probabilidad epsilon  -> EXPLORAR (probar algo distinto)
    ///   con probabilidad 1-epsilon -> EXPLOTAR (usar lo que ya funciona)
    ///
    /// Epsilon empieza alto (casi todo es exploracion, las celulas se ven
    /// caoticas) y va bajando ronda a ronda hasta el minimo.
    /// </summary>
    private int ChooseIndex(float[] table)
    {
        if (Random.value < Epsilon)
        {
            // --- EXPLORAR ---
            if (useLocalMutation && Random.value < 0.5f)
            {
                // Mutacion local: probamos una opcion vecina a la mejor
                // conocida. Sirve para afinar cuando ya sabemos por donde va.
                int best = BestIndex(table);
                int jump = Random.Range(-2, 3); // un numero entre -2 y 2
                return Wrap(best + jump, table.Length);
            }

            // Exploracion amplia: cualquier opcion.
            // Es imprescindible al principio, cuando todas las casillas valen
            // lo mismo y "la mejor" seria siempre la primera de la lista.
            return Random.Range(0, table.Length);
        }

        // --- EXPLOTAR ---
        return BestIndex(table);
    }

    /// <summary>Posicion de la casilla con el valor mas alto.</summary>
    private int BestIndex(float[] table)
    {
        int best = 0;
        for (int i = 1; i < table.Length; i++)
        {
            if (table[i] > table[best]) best = i;
        }
        return best;
    }

    /// <summary>Hace que la lista de colores se comporte como una rueda:
    /// pasado el ultimo se vuelve al primero.</summary>
    private int Wrap(int index, int length)
    {
        return ((index % length) + length) % length;
    }

    // ------------------------------------------------------------------
    //  APRENDER: actualizacion incremental
    // ------------------------------------------------------------------

    /// <summary>
    /// La formula del aprendizaje:
    ///
    ///     Q[accion] = Q[accion] + alfa * (R - Q[accion])
    ///
    /// Es la actualizacion incremental de media muestral
    /// (Sutton y Barto, Reinforcement Learning: An Introduction).
    ///
    /// Se lee asi: mueve el valor guardado un poquito hacia el resultado
    /// que acabas de ver. Cuanto es "un poquito" lo decide alfa.
    ///
    /// Ejemplo: Q[azul] = 0.40, la celula sobrevive entera (R = 1)
    ///          Q[azul] = 0.40 + 0.2 * (1 - 0.40) = 0.52
    /// </summary>
    public override void RegisterResult(CellGenome genome, float survivalRatio)
    {
        if (QColor == null) ResetLearning();

        int c = Mathf.Clamp(genome.colorIndex, 0, QColor.Length - 1);
        int s = Mathf.Clamp(genome.sizeIndex, 0, QSize.Length - 1);

        QColor[c] += alpha * (survivalRatio - QColor[c]);
        QSize[s] += alpha * (survivalRatio - QSize[s]);

        // Estadisticas para el CSV.
        roundTotal++;
        if (survivalRatio >= 0.999f) roundSurvivors++;
    }

    /// <summary>
    /// Cierra la ronda. La llama el CellSpawner DESPUES de haber reportado
    /// todas las celulas.
    ///
    /// Primero registramos la ronda con el epsilon que estuvo vigente
    /// durante ella, y recien despues lo hacemos decaer.
    /// </summary>
    public override void EndRound()
    {
        int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 0;

        if (logger != null)
        {
            logger.RecordRound(round, roundSurvivors, roundTotal, Epsilon);
        }

        float rate = roundTotal > 0 ? (float)roundSurvivors / roundTotal : 0f;
        Debug.Log("Ronda " + round
                + " | sobrevivieron " + roundSurvivors + "/" + roundTotal
                + " (" + rate.ToString("0.00") + ")"
                + " | epsilon " + Epsilon.ToString("0.00")
                + " | mejor color " + BestIndex(QColor)
                + " | mejor tamanio " + BestIndex(QSize));

        // Cada ronda exploramos un poco menos y explotamos un poco mas.
        Epsilon = Mathf.Max(minimumEpsilon, Epsilon * decayFactor);

        roundSurvivors = 0;
        roundTotal = 0;
    }

    // ------------------------------------------------------------------
    //  REINICIAR
    // ------------------------------------------------------------------

    /// <summary>
    /// Borra todo lo aprendido. La llama el GameManager al empezar una
    /// partida nueva: si no, la segunda partida arrancaria ya sabiendo la
    /// respuesta y no se veria aprender nada.
    ///
    /// Arrancamos en 0.5 (ni bien ni mal) para que ninguna opcion tenga
    /// ventaja de salida.
    /// </summary>
    public override void ResetLearning()
    {
        QColor = new float[colors.Length];
        QSize = new float[sizes.Length];

        for (int i = 0; i < QColor.Length; i++) QColor[i] = 0.5f;
        for (int i = 0; i < QSize.Length; i++) QSize[i] = 0.5f;

        Epsilon = startingEpsilon;

        roundSurvivors = 0;
        roundTotal = 0;

        if (logger != null) logger.StartNewRun();
    }

    // ------------------------------------------------------------------
    //  TRADUCCION de indice a valor real
    // ------------------------------------------------------------------

    public override Color GetColor(int colorIndex)
    {
        return colors[Mathf.Clamp(colorIndex, 0, colors.Length - 1)];
    }

    public override float GetSize(int sizeIndex)
    {
        return sizes[Mathf.Clamp(sizeIndex, 0, sizes.Length - 1)];
    }
}

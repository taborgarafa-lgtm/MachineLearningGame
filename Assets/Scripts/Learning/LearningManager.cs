using UnityEngine;

// Sistema de aprendizaje: bandido multibrazo con politica epsilon-greedy.
//
// Es aprendizaje por refuerzo. No hay datos etiquetados ni reglas escritas a
// mano sobre que color conviene: el sistema lo descubre por prueba y error a
// partir de una senial de recompensa.
//
//   Agente     -> la poblacion de celulas
//   Accion     -> elegir un color y un tamanio
//   Entorno    -> el fondo de la escena y el jugador
//   Recompensa -> fraccion de la ronda que la celula sobrevivio (0 a 1)
//
// Se usan dos tablas separadas y no una combinada de 8x4 = 32 casillas porque
// con 8 celulas por ronda llenar 32 casillas necesitaria mas de 40 rondas,
// mientras que 8 + 4 convergen en 10 o 15. La partida dura 15.
public class LearningManager : LearningManagerBase
{
    [Header("Opciones de color")]
    [Tooltip("El indice 4 es turquesa, parecido al fondo.")]
    public Color[] colors = new Color[]
    {
        new Color(0.85f, 0.35f, 0.35f), // 0 rojo
        new Color(0.85f, 0.60f, 0.30f), // 1 naranja
        new Color(0.85f, 0.85f, 0.35f), // 2 amarillo
        new Color(0.40f, 0.75f, 0.40f), // 3 verde
        new Color(0.30f, 0.70f, 0.65f), // 4 turquesa
        new Color(0.35f, 0.50f, 0.85f), // 5 azul
        new Color(0.60f, 0.40f, 0.85f), // 6 violeta
        new Color(0.85f, 0.40f, 0.65f)  // 7 rosa
    };

    [Header("Opciones de tamanio")]
    [Tooltip("Limites del enunciado: minimo 0.5, maximo 1.25.")]
    public float[] sizes = new float[] { 0.5f, 0.75f, 1.0f, 1.25f };

    [Header("Parametros del aprendizaje")]
    [Tooltip("Cuanto pesa cada resultado nuevo frente a lo ya aprendido.")]
    [Range(0.01f, 1f)] public float alpha = 0.2f;

    [Tooltip("Probabilidad de explorar en la primera ronda.")]
    [Range(0f, 1f)] public float startingEpsilon = 0.9f;

    [Tooltip("Epsilon nunca baja de aqui: siempre queda algo de exploracion.")]
    [Range(0f, 1f)] public float minimumEpsilon = 0.1f;

    [Tooltip("Cuanto se multiplica epsilon al cerrar cada ronda.")]
    [Range(0.5f, 1f)] public float decayFactor = 0.9f;

    [Tooltip("Parte de la exploracion prueba colores vecinos al mejor conocido.")]
    public bool useLocalMutation = true;

    [Header("Opcional")]
    public SurvivalLogger logger;

    public float[] QColor { get; private set; }
    public float[] QSize { get; private set; }
    public float Epsilon { get; private set; }

    private int roundSurvivors;
    private int roundTotal;

    public override int ColorCount => colors.Length;
    public override int SizeCount => sizes.Length;

    private void Awake()
    {
        ResetLearning();
    }

    public override CellGenome SelectGenome()
    {
        return new CellGenome(ChooseIndex(QColor), ChooseIndex(QSize));
    }

    // El dilema exploracion / explotacion:
    //   con probabilidad epsilon    -> probar algo distinto
    //   con probabilidad 1-epsilon  -> usar lo que mejor ha funcionado
    private int ChooseIndex(float[] table)
    {
        if (Random.value < Epsilon)
        {
            if (useLocalMutation && Random.value < 0.5f)
            {
                // Busqueda local: probar una opcion vecina a la mejor conocida.
                int best = BestIndex(table);
                int jump = Random.Range(-2, 3);
                return Wrap(best + jump, table.Length);
            }

            // Exploracion amplia. Imprescindible al principio, cuando todas las
            // casillas valen lo mismo y la "mejor" seria siempre la primera.
            return Random.Range(0, table.Length);
        }

        return BestIndex(table);
    }

    private int BestIndex(float[] table)
    {
        int best = 0;
        for (int i = 1; i < table.Length; i++)
        {
            if (table[i] > table[best]) best = i;
        }
        return best;
    }

    // Hace que la lista de colores se comporte como una rueda.
    private int Wrap(int index, int length)
    {
        return ((index % length) + length) % length;
    }

    // Actualizacion incremental de media muestral (Sutton y Barto):
    //
    //     Q[accion] = Q[accion] + alfa * (R - Q[accion])
    //
    // Mueve el valor guardado un poco hacia el resultado recien observado.
    // Ejemplo: Q = 0.40 y la celula sobrevive entera (R = 1)
    //          Q = 0.40 + 0.2 * (1 - 0.40) = 0.52
    public override void RegisterResult(CellGenome genome, float survivalRatio)
    {
        if (QColor == null) ResetLearning();

        int c = Mathf.Clamp(genome.colorIndex, 0, QColor.Length - 1);
        int s = Mathf.Clamp(genome.sizeIndex, 0, QSize.Length - 1);

        QColor[c] += alpha * (survivalRatio - QColor[c]);
        QSize[s] += alpha * (survivalRatio - QSize[s]);

        roundTotal++;
        if (survivalRatio >= 0.999f) roundSurvivors++;
    }

    public override void EndRound()
    {
        int round = GameManager.Instance != null ? GameManager.Instance.CurrentRound : 0;

        // Se registra con el epsilon vigente durante la ronda, antes de decaerlo.
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

        // Cada ronda se explora un poco menos y se explota un poco mas.
        Epsilon = Mathf.Max(minimumEpsilon, Epsilon * decayFactor);

        roundSurvivors = 0;
        roundTotal = 0;
    }

    public override void ResetLearning()
    {
        QColor = new float[colors.Length];
        QSize = new float[sizes.Length];

        // Arrancan en 0.5, ni bien ni mal, para que ninguna opcion
        // tenga ventaja de salida.
        for (int i = 0; i < QColor.Length; i++) QColor[i] = 0.5f;
        for (int i = 0; i < QSize.Length; i++) QSize[i] = 0.5f;

        Epsilon = startingEpsilon;

        roundSurvivors = 0;
        roundTotal = 0;

        if (logger != null) logger.StartNewRun();
    }

    public override Color GetColor(int colorIndex)
    {
        return colors[Mathf.Clamp(colorIndex, 0, colors.Length - 1)];
    }

    public override float GetSize(int sizeIndex)
    {
        return sizes[Mathf.Clamp(sizeIndex, 0, sizes.Length - 1)];
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Guarda la tasa de supervivencia de cada ronda en un CSV.
///
/// POR QUE IMPORTA: con este archivo se hace un grafico que demuestra
/// NUMERICAMENTE que el sistema aprende (la linea sube ronda a ronda).
/// Eso es literalmente lo que pide el criterio de evaluacion "correcta
/// implementacion del sistema de adaptacion", y casi ningun equipo lo
/// entrega. Son unas pocas lineas de codigo.
///
/// El archivo usa punto y coma como separador y coma decimal, que es el
/// formato que espera Excel en espaniol: se abre directo, sin importar nada.
/// </summary>
public class SurvivalLogger : MonoBehaviour
{
    [Tooltip("Nombre del archivo que se genera.")]
    public string fileName = "supervivencia.csv";

    [Tooltip("Si esta activo, guarda el archivo solo al terminar la partida.")]
    public bool saveOnGameEnd = true;

    private readonly List<string> rows = new List<string>();

    private void Start()
    {
        if (saveOnGameEnd && GameManager.Instance != null)
        {
            GameManager.Instance.OnGameEnd += Save;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameEnd -= Save;
    }

    /// <summary>Empieza una tanda nueva. La llama LearningManager.ResetLearning().</summary>
    public void StartNewRun()
    {
        rows.Clear();
        rows.Add("ronda;supervivientes;total;tasa;epsilon");
    }

    /// <summary>Anota una ronda. La llama LearningManager.EndRound().</summary>
    public void RecordRound(int round, int survivors, int total, float epsilon)
    {
        if (rows.Count == 0) StartNewRun();

        float rate = total > 0 ? (float)survivors / total : 0f;

        rows.Add(round + ";" + survivors + ";" + total + ";"
               + Decimal3(rate) + ";" + Decimal3(epsilon));
    }

    /// <summary>Escribe el archivo en disco.</summary>
    public void Save()
    {
        if (rows.Count <= 1)
        {
            Debug.LogWarning("SurvivalLogger: no hay rondas que guardar.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllText(path, string.Join("\n", rows), Encoding.UTF8);

        Debug.Log("CSV guardado en: " + path);
    }

    /// <summary>Tres decimales con coma, para que Excel en espaniol lo lea bien.</summary>
    private string Decimal3(float value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}

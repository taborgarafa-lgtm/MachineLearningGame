using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// Guarda la tasa de supervivencia de cada ronda en un CSV. Con ese archivo se
// hace el grafico que demuestra numericamente que el sistema aprende.
//
// Separador de punto y coma y coma decimal: el formato que espera Excel en
// espaniol, asi se abre directo sin importar nada.
public class SurvivalLogger : MonoBehaviour
{
    public string fileName = "supervivencia.csv";

    [Tooltip("Guardar el archivo al terminar la partida.")]
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

    // La llama LearningManager.ResetLearning().
    public void StartNewRun()
    {
        rows.Clear();
        rows.Add("ronda;supervivientes;total;tasa;epsilon");
    }

    // La llama LearningManager.EndRound().
    public void RecordRound(int round, int survivors, int total, float epsilon)
    {
        if (rows.Count == 0) StartNewRun();

        float rate = total > 0 ? (float)survivors / total : 0f;

        rows.Add(round + ";" + survivors + ";" + total + ";"
               + Decimal3(rate) + ";" + Decimal3(epsilon));
    }

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

    private string Decimal3(float value)
    {
        return value.ToString("0.000", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Panel de debug: muestra en pantalla el valor de cada casilla de las
/// tablas de aprendizaje, en forma de barras.
///
/// POR QUE IMPORTA: es lo que hace que el aprendizaje SE VEA en el video
/// en vez de tener que explicarlo con palabras. Las barras crecen y decaen
/// en vivo mientras se juega. Cuenta para el criterio de innovacion.
///
/// Esta hecho con OnGUI a proposito: no necesita Canvas, ni prefabs, ni
/// arrastrar nada. Se aniade el componente y ya funciona. Es un overlay de
/// depuracion, no UI del juego, asi que no vale la pena montarlo con uGUI.
/// </summary>
public class LearningDebugPanel : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aqui el objeto que tiene el LearningManager.")]
    public LearningManager learning;

    [Header("Configuracion")]
    public bool visible = true;

    [Tooltip("Tecla para mostrar y ocultar el panel.")]
    public Key toggleKey = Key.Tab;

    private const float BarWidth = 26f;
    private const float BarGap = 6f;
    private const float MaxBarHeight = 110f;

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current[toggleKey].wasPressedThisFrame) visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (learning == null || learning.QColor == null || learning.QSize == null) return;

        float baseline = Screen.height - 34f;
        float left = 24f;

        float colorsWidth = learning.QColor.Length * (BarWidth + BarGap);
        float sizesWidth = learning.QSize.Length * (BarWidth + BarGap);
        float panelWidth = colorsWidth + sizesWidth + 60f;

        // Fondo oscuro semitransparente para que se lea sobre cualquier color.
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(left - 12f, baseline - MaxBarHeight - 44f,
                                 panelWidth, MaxBarHeight + 62f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(left, baseline - MaxBarHeight - 42f, 460f, 20f),
                  "APRENDIZAJE    epsilon " + learning.Epsilon.ToString("0.00")
                  + "    (" + toggleKey + " para ocultar)");

        // --- Barras de color ---
        DrawBars(learning.QColor, left, baseline, true);

        // --- Barras de tamanio, a la derecha ---
        float sizesLeft = left + colorsWidth + 36f;
        DrawBars(learning.QSize, sizesLeft, baseline, false);

        GUI.Label(new Rect(left, baseline + 16f, 200f, 20f), "colores");
        GUI.Label(new Rect(sizesLeft, baseline + 16f, 200f, 20f), "tamanios");
    }

    /// <summary>
    /// Dibuja una barra por casilla. La altura es proporcional al valor Q:
    /// barra alta = esa opcion ha sobrevivido bien hasta ahora.
    /// </summary>
    private void DrawBars(float[] table, float left, float baseline, bool useColors)
    {
        for (int i = 0; i < table.Length; i++)
        {
            float q = Mathf.Clamp01(table[i]);
            float height = Mathf.Max(2f, q * MaxBarHeight);
            float x = left + i * (BarWidth + BarGap);

            GUI.color = useColors && i < learning.colors.Length
                ? learning.colors[i]
                : new Color(0.75f, 0.75f, 0.75f);

            GUI.DrawTexture(new Rect(x, baseline - height, BarWidth, height),
                            Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(new Rect(x - 3f, baseline + 1f, BarWidth + 10f, 18f),
                      q.ToString("0.00"));
        }
    }
}

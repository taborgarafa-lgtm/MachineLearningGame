using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Herramienta de editor: arma la escena entera desde el menu
// Cells Catcher > Construir escena.
//
// Crea la capa, configura la camara, monta el objeto GameManager con sus
// componentes ya enlazados y construye el Canvas con el HUD y la pantalla
// final. Se puede ejecutar varias veces: reutiliza lo que ya existe.
//
// Esta en una carpeta Editor, asi que Unity la excluye de la build final.
//
// Requiere los TMP Essential Resources importados y el prefab en
// Assets/Prefabs/Cell.prefab.
public static class SceneBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Cell.prefab";
    private const string LayerName = "Celulas";

    // #338C85 en valores de 0 a 1.
    private static readonly Color BackgroundColor = new Color(0.20f, 0.55f, 0.52f);

    [MenuItem("Cells Catcher/Construir escena")]
    public static void Build()
    {
        Cell prefab = AssetDatabase.LoadAssetAtPath<Cell>(PrefabPath);

        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Falta el prefab de la celula",
                "No encontre " + PrefabPath + ".\n\n" +
                "Creralo asi:\n" +
                "1. GameObject > 2D Object > Sprites > Circle\n" +
                "2. Renombralo Cell\n" +
                "3. Anadele Circle Collider 2D y el script Cell\n" +
                "4. Arrastralo a la carpeta Assets/Prefabs\n" +
                "5. Borralo de la escena\n\n" +
                "Despues vuelve a ejecutar esta opcion.",
                "Entendido");
            return;
        }

        int layer = EnsureLayer(LayerName);
        AssignLayerToPrefab(prefab, layer);

        ConfigureCamera();
        GameObject manager = BuildManager(prefab, layer);
        BuildCanvas();
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = manager;

        Debug.Log("Escena construida. Guarda con Ctrl+S y dale a Play.");
    }

    // ------------------------------------------------------------------
    //  Capa
    // ------------------------------------------------------------------

    // Crea la capa si no existe y devuelve su numero.
    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing != -1) return existing;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0)
        {
            Debug.LogWarning("No pude abrir TagManager. Crea la capa " + name + " a mano.");
            return 0;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Las capas 0 a 7 son de Unity; las libres empiezan en la 8.
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = name;
                tagManager.ApplyModifiedProperties();
                Debug.Log("Capa " + name + " creada en la posicion " + i + ".");
                return i;
            }
        }

        Debug.LogWarning("No quedan capas libres. Crea " + name + " a mano.");
        return 0;
    }

    private static void AssignLayerToPrefab(Cell prefab, int layer)
    {
        if (prefab.gameObject.layer == layer) return;

        prefab.gameObject.layer = layer;
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------
    //  Camara
    // ------------------------------------------------------------------

    // El fondo es el color de la camara. Un sprite de fondo seria un objeto
    // mas que dibujar cada frame para conseguir exactamente lo mismo.
    private static void ConfigureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No hay camara con la etiqueta MainCamera.");
            return;
        }

        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BackgroundColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
    }

    // ------------------------------------------------------------------
    //  GameManager
    // ------------------------------------------------------------------

    private static GameObject BuildManager(Cell prefab, int layer)
    {
        GameObject go = GameObject.Find("GameManager");
        if (go == null)
        {
            go = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(go, "Crear GameManager");
        }

        GameManager manager = GetOrAdd<GameManager>(go);
        DummyLearning learning = GetOrAdd<DummyLearning>(go);
        CellPool pool = GetOrAdd<CellPool>(go);
        CellSpawner spawner = GetOrAdd<CellSpawner>(go);
        CellClicker clicker = GetOrAdd<CellClicker>(go);

        manager.roundDuration = 10f;
        manager.totalRounds = 15;
        manager.learning = learning;

        pool.cellPrefab = prefab;
        pool.prewarmCount = 20;

        spawner.cellPrefab = prefab;
        spawner.pool = pool;
        spawner.cellsPerRound = 8;
        spawner.maxCellsOnScreen = 20;
        spawner.usePooling = true;

        // La mascara de capas es un bit por capa: desplazamos un 1 hasta
        // la posicion de nuestra capa para marcar solo esa.
        clicker.cellLayer = 1 << layer;
        clicker.pointsPerCell = 1;

        return go;
    }

    // ------------------------------------------------------------------
    //  Interfaz
    // ------------------------------------------------------------------

    private static void BuildCanvas()
    {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Crear Canvas");
        }

        Canvas canvas = GetOrAdd<Canvas>(canvasGo);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GetOrAdd<GraphicRaycaster>(canvasGo);

        Transform parent = canvasGo.transform;

        // --- HUD ---
        TextMeshProUGUI round = MakeText(parent, "RoundText",
            new Vector2(0f, 1f), new Vector2(40f, -30f), "Ronda 1 / 15",
            TextAlignmentOptions.TopLeft);

        TextMeshProUGUI timer = MakeText(parent, "TimerText",
            new Vector2(0.5f, 1f), new Vector2(0f, -30f), "10s",
            TextAlignmentOptions.Top);

        TextMeshProUGUI score = MakeText(parent, "ScoreText",
            new Vector2(1f, 1f), new Vector2(-40f, -30f), "Puntaje: 0",
            TextAlignmentOptions.TopRight);

        HUDController hud = GetOrAdd<HUDController>(canvasGo);
        hud.roundText = round;
        hud.timerText = timer;
        hud.scoreText = score;

        // --- Pantalla final ---
        GameObject panel = FindOrCreateChild(parent, "EndPanel");
        RectTransform panelRect = GetOrAdd<RectTransform>(panel);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = GetOrAdd<Image>(panel);
        panelImage.color = new Color(0f, 0f, 0f, 0.8f);

        TextMeshProUGUI finalScore = MakeText(panel.transform, "FinalScoreText",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), "Puntaje final: 0",
            TextAlignmentOptions.Center, 64);

        TextMeshProUGUI rounds = MakeText(panel.transform, "RoundsText",
            new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), "Rondas jugadas: 15",
            TextAlignmentOptions.Center);

        Button playAgain = MakeButton(panel.transform, "PlayAgainButton",
            new Vector2(0f, -70f), "Volver a jugar");

        EndScreenController endScreen = GetOrAdd<EndScreenController>(canvasGo);
        endScreen.panel = panel;
        endScreen.finalScoreText = finalScore;
        endScreen.roundsText = rounds;
        endScreen.playAgainButton = playAgain;

        panel.SetActive(true); // el script lo oculta solo al arrancar
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name,
        Vector2 anchor, Vector2 position, string text,
        TextAlignmentOptions alignment, int fontSize = 44)
    {
        GameObject go = FindOrCreateChild(parent, name);

        RectTransform rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(600f, 80f);

        TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(go);
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;

        return label;
    }

    private static Button MakeButton(Transform parent, string name,
        Vector2 position, string text)
    {
        GameObject go = FindOrCreateChild(parent, name);

        RectTransform rect = GetOrAdd<RectTransform>(go);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(340f, 90f);

        Image image = GetOrAdd<Image>(go);
        image.color = new Color(0.30f, 0.70f, 0.65f);

        Button button = GetOrAdd<Button>(go);
        button.targetGraphic = image;

        TextMeshProUGUI label = MakeText(go.transform, "Label",
            new Vector2(0.5f, 0.5f), Vector2.zero, text,
            TextAlignmentOptions.Center, 38);
        label.rectTransform.sizeDelta = rect.sizeDelta;
        label.color = Color.black;

        return button;
    }

    // ------------------------------------------------------------------
    //  EventSystem
    // ------------------------------------------------------------------

    // Sin EventSystem los botones no responden, y como el proyecto usa el
    // Input System nuevo hace falta su modulo, no el Standalone.
    private static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>();

        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;

            // Si tiene el modulo viejo lo quitamos: da error con el Input System nuevo.
            StandaloneInputModule old = go.GetComponent<StandaloneInputModule>();
            if (old != null) Object.DestroyImmediate(old);
        }
        else
        {
            go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            Undo.RegisterCreatedObjectUndo(go, "Crear EventSystem");
        }

        GetOrAdd<InputSystemUIInputModule>(go);
    }

    // ------------------------------------------------------------------
    //  Utilidades
    // ------------------------------------------------------------------

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
        return go;
    }
}

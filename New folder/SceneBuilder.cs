#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the entire demo scene through Unity's real API instead of a
/// hand-written .unity file, so all script/component references are
/// resolved correctly by your project's actual installed packages.
///
/// Usage: Tools > Sight Game > Build Demo Scene.
/// See GDD.md section 7 for setup requirements (2D URP template etc).
/// </summary>
public static class SceneBuilder
{
    private const float CellSize = 1.6f;

    // Maze legend: # wall, . floor, S player spawn, E exit, X enemy spawn.
    // Matches GDD.md section 5.
    private static readonly string[] MazeLayout =
    {
        "###########",
        "#S....#X..#",
        "#####.###.#",
        "#...#..X#.#",
        "#.#.###.#.#",
        "#.#..X#...#",
        "#.#######.#",
        "#..X.....E#",
        "###########",
    };

    [MenuItem("Tools/Sight Game/Build Demo Scene")]
    public static void BuildScene()
    {
        EnsureTag("Player");
        int wallLayer = EnsureLayer("Wall");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneAddMode.Single);

        Sprite squareSprite = CreateWhiteSquareSpriteAsset();

        // --- Camera -----------------------------------------------------
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // --- Lights (best effort via reflection, see TryAddLight2D) -----
        GameObject globalLightGO = new GameObject("GlobalLight2D");
        TryAddLight2D(globalLightGO, "Global", 0.04f, Color.white, -1, -1, -1);

        GameObject visionLightGO = new GameObject("VisionLight");
        TryAddLight2D(visionLightGO, "Point", 1.6f, new Color(1f, 0.95f, 0.8f), 2.4f, 0f, 1f);

        // --- Managers -----------------------------------------------------
        GameObject managersGO = new GameObject("Managers");
        GameManager gameManager = managersGO.AddComponent<GameManager>();
        GazeTracker gazeTracker = managersGO.AddComponent<GazeTracker>();
        VisionController visionController = managersGO.AddComponent<VisionController>();

        visionController.gazeTracker = gazeTracker;
        visionController.mainCamera = cam;
        visionController.visionLight = visionLightGO.transform;
        visionController.visionRadius = 2.2f;

        gameManager.gazeTracker = gazeTracker;

        // --- Maze, player, enemy spawns, exit ----------------------------
        GameObject mazeRoot = new GameObject("Maze");
        Transform player = null;
        List<Vector3> enemySpawnPositions = new List<Vector3>();
        Vector3 exitPos = Vector3.zero;

        int rows = MazeLayout.Length;
        int cols = MazeLayout[0].Length;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                char c = MazeLayout[row][col];
                Vector3 pos = new Vector3(col * CellSize, (rows - 1 - row) * CellSize, 0f);

                if (c == '#')
                {
                    CreateWallTile(mazeRoot.transform, pos, squareSprite, wallLayer);
                    continue;
                }

                CreateFloorTile(mazeRoot.transform, pos, squareSprite);

                if (c == 'S')
                    player = CreatePlayer(pos, squareSprite);
                else if (c == 'E')
                    exitPos = pos;
                else if (c == 'X')
                    enemySpawnPositions.Add(pos);
            }
        }

        // --- Sanity manager ----------------------------------------------
        SanityManager sanityManager = managersGO.AddComponent<SanityManager>();
        sanityManager.player = player;
        sanityManager.vision = visionController;

        // --- Exit ----------------------------------------------------------
        GameObject exitGO = new GameObject("Exit");
        exitGO.transform.position = exitPos;
        exitGO.transform.localScale = Vector3.one * CellSize * 0.8f;
        var exitSr = exitGO.AddComponent<SpriteRenderer>();
        exitSr.sprite = squareSprite;
        exitSr.color = new Color(0.2f, 1f, 0.4f, 0.65f);
        exitSr.sortingOrder = 2;
        var exitCol = exitGO.AddComponent<BoxCollider2D>();
        exitCol.isTrigger = true;
        exitGO.AddComponent<ExitTrigger>();

        // --- Enemies ---------------------------------------------------
        LayerMask wallMask = 1 << wallLayer;
        foreach (Vector3 spawnPos in enemySpawnPositions)
        {
            GameObject enemyGO = new GameObject("Enemy");
            enemyGO.transform.position = spawnPos;
            enemyGO.transform.localScale = Vector3.one * CellSize * 0.7f;
            var sr = enemyGO.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.color = new Color(0.9f, 0.15f, 0.2f);
            sr.sortingOrder = 4;

            var rb = enemyGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            enemyGO.AddComponent<CircleCollider2D>();

            var ai = enemyGO.AddComponent<EnemyAI>();
            ai.player = player;
            ai.vision = visionController;
            ai.wallLayer = wallMask;
        }

        // --- Camera framing (static camera over the whole maze) --------
        cam.transform.position = new Vector3((cols * CellSize) / 2f, (rows * CellSize) / 2f, -10f);
        cam.orthographicSize = (rows * CellSize) / 2f * 1.15f;

        // --- UI ----------------------------------------------------------
        BuildUI(sanityManager, gameManager);

        // --- Save ----------------------------------------------------------
        string scenesFolder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesFolder))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, scenesFolder + "/DemoLevel.unity");

        Debug.Log("Demo scene built and saved to Assets/Scenes/DemoLevel.unity. " +
            "If you saw a Light2D warning above, the Universal RP package wasn't detected — " +
            "add Light2D components manually to GlobalLight2D (type Global, low intensity) " +
            "and VisionLight (type Point, outer radius ~2.2) and everything else will still work.");
    }

    // ------------------------------------------------------------------
    // Tile / entity creation helpers
    // ------------------------------------------------------------------

    private static void CreateWallTile(Transform parent, Vector3 pos, Sprite sprite, int wallLayer)
    {
        GameObject go = new GameObject("Wall");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * CellSize;
        go.layer = wallLayer;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.22f, 0.22f, 0.28f);
        sr.sortingOrder = 1;

        go.AddComponent<BoxCollider2D>();
    }

    private static void CreateFloorTile(Transform parent, Vector3 pos, Sprite sprite)
    {
        GameObject go = new GameObject("Floor");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * CellSize;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.45f, 0.45f, 0.5f);
        sr.sortingOrder = 0;
    }

    private static Transform CreatePlayer(Vector3 pos, Sprite sprite)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * CellSize * 0.6f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.3f, 0.6f, 1f);
        sr.sortingOrder = 5;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<PlayerController>();

        return go.transform;
    }

    // ------------------------------------------------------------------
    // UI
    // ------------------------------------------------------------------

    private static void BuildUI(SanityManager sanityManager, GameManager gameManager)
    {
        GameObject canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Sanity bar
        GameObject sliderGO = new GameObject("SanitySlider");
        sliderGO.transform.SetParent(canvasGO.transform);
        var rect = sliderGO.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.02f, 0.92f);
        rect.anchorMax = new Vector2(0.32f, 0.97f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGO.transform);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero; fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero; fillAreaRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        var fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.8f, 0.2f, 0.3f);

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImg;

        sanityManager.sanitySlider = slider;

        // Panels
        gameManager.calibrationPanel = BuildPanel(canvasGO.transform, "CalibrationPanel",
            "Look straight at the webcam.\nPress SPACE to calibrate and start.");
        gameManager.gameOverPanel = BuildPanel(canvasGO.transform, "GameOverPanel", "GAME OVER");
        gameManager.winPanel = BuildPanel(canvasGO.transform, "WinPanel", "YOU ESCAPED");

        gameManager.gameOverPanel.SetActive(false);
        gameManager.winPanel.SetActive(false);
    }

    private static GameObject BuildPanel(Transform parent, string name, string message)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        var img = panel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(panel.transform);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.4f);
        textRect.anchorMax = new Vector2(0.9f, 0.6f);
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        var text = textGO.AddComponent<Text>();
        text.text = message;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 32;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return panel;
    }

    // ------------------------------------------------------------------
    // Placeholder sprite generation (no external assets required)
    // ------------------------------------------------------------------

    private static Sprite CreateWhiteSquareSpriteAsset()
    {
        const string folder = "Assets/Generated";
        const string pngPath = folder + "/WhiteSquare.png";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Generated");

        if (!File.Exists(pngPath))
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(pngPath);
        }

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 4;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }

    // ------------------------------------------------------------------
    // Light2D via reflection (avoids a hard compile-time dependency on
    // the Universal RP package, in case it isn't installed yet).
    // ------------------------------------------------------------------

    private static void TryAddLight2D(GameObject go, string lightTypeName, float intensity, Color color,
        float outerRadius, float innerRadius, float falloff)
    {
        Type light2DType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (light2DType == null)
        {
            Debug.LogWarning($"Light2D type not found while setting up '{go.name}'. " +
                "Install the Universal RP package (2D Renderer) and add a Light2D component manually: " +
                $"{lightTypeName} type, intensity {intensity}.");
            return;
        }

        Component light = go.AddComponent(light2DType);

        SetPropertySafe(light, "intensity", intensity);
        SetPropertySafe(light, "color", color);
        SetEnumPropertySafe(light, "lightType", lightTypeName);

        if (lightTypeName == "Point")
        {
            if (outerRadius >= 0) SetPropertySafe(light, "pointLightOuterRadius", outerRadius);
            if (innerRadius >= 0) SetPropertySafe(light, "pointLightInnerRadius", innerRadius);
        }
        if (falloff >= 0) SetPropertySafe(light, "falloffIntensity", falloff);
    }

    private static void SetPropertySafe(object obj, string propName, object value)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName);
            if (prop != null && prop.CanWrite) prop.SetValue(obj, value);
        }
        catch
        {
            Debug.LogWarning($"Could not set Light2D property '{propName}' via reflection " +
                "(API may differ slightly across URP versions). Set it manually in the Inspector if needed.");
        }
    }

    private static void SetEnumPropertySafe(object obj, string propName, string enumValueName)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propName);
            if (prop == null) return;
            var enumValue = Enum.Parse(prop.PropertyType, enumValueName);
            prop.SetValue(obj, enumValue);
        }
        catch
        {
            Debug.LogWarning($"Could not set Light2D lightType to '{enumValueName}' via reflection. " +
                "Set it manually in the Inspector if needed.");
        }
    }

    // ------------------------------------------------------------------
    // Tags & Layers (project-wide settings, created if missing)
    // ------------------------------------------------------------------

    private static void EnsureTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }

        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }

    private static int EnsureLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 8; i < layersProp.arraySize; i++) // user layers start at index 8
        {
            if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
                return i;
        }

        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerSP.stringValue))
            {
                layerSP.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning($"No free layer slot found for '{layerName}', defaulting to layer 0.");
        return 0;
    }
}
#endif

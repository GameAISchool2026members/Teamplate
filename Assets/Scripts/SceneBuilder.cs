using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Builds all five game scenes through Unity's API.
/// Menu: Tools > Sight Game > Build All Scenes
/// See GDD.md section 7 for setup (requires a 2D URP project).
/// </summary>
public static class SceneBuilder
{
    // ------------------------------------------------------------------ palette
    static Color BG      = H("#0A0A0E");
    static Color TILE    = H("#AAAACC"); // used at very low alpha for grid
    static Color CORNER  = H("#3A3050");
    static Color GOLD1   = H("#E8D8A0"); // title
    static Color GOLD2   = H("#7A6A50"); // subtitle / muted text
    static Color GOLD3   = H("#E8C870"); // primary button text
    static Color GOLD4   = H("#C8A840"); // primary button border
    static Color BTN_BG  = H("#0F0D18"); // button background
    static Color BTN_BD  = H("#3A3050"); // standard button border
    static Color BTN_TXT = H("#C8B8E0"); // standard button text
    static Color TEAL_BD = H("#2A5A6A"); // eye button border
    static Color TEAL_TX = H("#70C0D8"); // eye button text
    static Color OPT_LBL = H("#9A8A80");
    static Color OPT_VAL = H("#C8B8A0");
    static Color RED_DRK = H("#8A1010"); // game over title
    static Color RED_MED = H("#C05050"); // game over stat
    static Color RED_BG  = H("#0A0204"); // game over bg
    static Color AMB_TTL = H("#C8A030"); // win title
    static Color AMB_STA = H("#C8901A"); // win stat
    static Color AMB_BG  = H("#0A0A04"); // win bg
    static Color FLAME   = H("#F0A030"); // torch flame
    static Color TORCH   = H("#C8761A"); // torch body

    static Color PANEL_BG = new Color(0.06f, 0.05f, 0.10f, 0.97f);

    static Color H(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    // ------------------------------------------------------------------ maze layout
    static readonly string[] Maze =
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
    const float CellSize = 1.6f;

    // ================================================================== MENU ITEM
    [MenuItem("Tools/Sight Game/Build All Scenes")]
    public static void BuildAllScenes()
    {
        EnsureTag("Player");
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Generated");

        Sprite sq = CreateWhiteSquare();

        BuildMainMenuScene(sq);
        // Eye calibration now uses the UnitEye package scene (GazeCalibration).
        // DemoLevel is currently not used in runtime flow; keep existing GameScene instead.
        BuildGameOverScene(sq);
        BuildWinScene(sq);
        RegisterBuildSettings();

        Debug.Log("Core scenes rebuilt and Build Settings now point to GameScene. " +
                  "Remember to import Cinzel font into Assets/Resources/ for best visuals.");
    }

    // ================================================================== 1. MAIN MENU
    static void BuildMainMenuScene(Sprite sq)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureEventSystem();

        // Camera
        GameObject camGO = new GameObject("MainCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG;
        cam.orthographic = true;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        // Canvas
        var (canvas, cRect) = MakeCanvas();

        // Background
        Image bgImage = MakeImage(canvas, "BG", BG, V(0, 0), V(1, 1), Vector2.zero, Vector2.zero, 0);
        Sprite bgSprite = LoadMainMenuBackgroundSprite();
        if (bgSprite != null)
        {
            bgImage.sprite = bgSprite;
            bgImage.color = Color.white;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = false;
        }

        // Corner decorations
        AddCorners(canvas, CORNER, 0);

        // Torches (4 corners)
        AddTorch(canvas, "TorchTL", FLAME, TORCH, V(0,1), V(0,1), new Vector2( 28, -28));
        AddTorch(canvas, "TorchTR", FLAME, TORCH, V(1,1), V(1,1), new Vector2(-28, -28));
        AddTorch(canvas, "TorchBL", FLAME, TORCH, V(0,0), V(0,0), new Vector2( 28,  28));
        AddTorch(canvas, "TorchBR", FLAME, TORCH, V(1,0), V(1,0), new Vector2(-28,  28));

        // Decorative sanity bar at top center
        GameObject sanDeco = MakeGO("SanityBarDeco", canvas);
        SetRT(sanDeco, V(.5f,1f), V(.5f,1f), new Vector2(0,-26), new Vector2(160,6));
        MakeImageOnGO(sanDeco, new Color(0.1f,0.06f,0.18f));
        GameObject sanFill = MakeGO("Fill", sanDeco.transform);
        SetRT(sanFill, V(0,0), V(.4f,1f), Vector2.zero, Vector2.zero);
        MakeImageOnGO(sanFill, H("#6A3070"));

        // Title
        Text title = MakeText(canvas, "Title", "S I G H T", 80, GOLD1,
            V(.5f,.65f), V(.5f,.65f), Vector2.zero, new Vector2(800,100));
        title.fontStyle = FontStyle.Bold;

        // Subtitle
        MakeText(canvas, "Subtitle", "YOU CANNOT LOOK AWAY", 22, GOLD2,
            V(.5f,.57f), V(.5f,.57f), Vector2.zero, new Vector2(700,40));

        // Ornament line
        MakeImage(canvas, "Orn", new Color(0.28f,0.22f,0.16f,1f),
            V(.5f,.52f), V(.5f,.52f), new Vector2(-80,0), new Vector2(80,0), -1, new Vector2(160,1));

        // Main panel with 3 buttons
        GameObject mainPanel = MakeGO("MainPanel", canvas);
        SetRT(mainPanel, V(.5f,.37f), V(.5f,.37f), Vector2.zero, new Vector2(420,170));

        Button startBtn = MakeMenuButton(mainPanel.transform, "StartGameBtn",
            "> START GAME", BTN_BG, H("#1A1508"), H("#7A6A40"), GOLD3, new Vector2(0,55), new Vector2(380,50));
        Button optBtn = MakeMenuButton(mainPanel.transform, "OptionsBtn",
            "# OPTIONS", BTN_BG, H("#1A1530"), BTN_BD, BTN_TXT, new Vector2(0,0), new Vector2(380,50));
        Button eyeBtn = MakeMenuButton(mainPanel.transform, "EyeGazingBtn",
            "o EYE GAZING CONFIG", BTN_BG, H("#081518"), TEAL_BD, TEAL_TX, new Vector2(0,-55), new Vector2(380,50));

        // Version text
        MakeText(canvas, "Version", "v0.1 demo", 14, new Color(.22f,.20f,.30f),
            V(1,0), V(1,0), new Vector2(-14, 14), new Vector2(120,20), TextAnchor.LowerRight);

        // Options panel (initially hidden)
        GameObject optPanel = BuildOptionsPanel(canvas);
        optPanel.SetActive(false);

        // Manager + script
        GameObject mgr = new GameObject("MenuManager");
        MainMenuUI ui = mgr.AddComponent<MainMenuUI>();
        ui.mainPanel    = mainPanel;
        ui.optionsPanel = optPanel;
        ui.startGameBtn = startBtn;
        ui.optionsBtn   = optBtn;
        ui.eyeGazingBtn = eyeBtn;

        // Wire options panel references from its children
        WireOptionsPanel(optPanel, ui);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    static GameObject BuildOptionsPanel(Transform canvas)
    {
        GameObject panel = MakeGO("OptionsPanel", canvas);
        SetRT(panel, V(.5f,.5f), V(.5f,.5f), Vector2.zero, new Vector2(560,460));
        MakeImageOnGO(panel, PANEL_BG);

        // Border lines (top + bottom 1px)
        GameObject tb = MakeGO("BorderTop", panel.transform);
        SetRT(tb, V(0,1), V(1,1), new Vector2(0,-1), new Vector2(0,1));
        MakeImageOnGO(tb, CORNER);
        GameObject bb = MakeGO("BorderBot", panel.transform);
        SetRT(bb, V(0,0), V(1,0), new Vector2(0,0), new Vector2(0,1));
        MakeImageOnGO(bb, CORNER);

        // Title
        MakeText(panel.transform, "OptionsTitle", "OPTIONS", 28, H("#C8B090"),
            V(.5f,1f), V(.5f,1f), new Vector2(0,-32), new Vector2(400,36));

        // Separator
        GameObject sep = MakeGO("Sep", panel.transform);
        SetRT(sep, V(.1f,1f), V(.9f,1f), new Vector2(0,-56), new Vector2(0,1));
        MakeImageOnGO(sep, CORNER);

        // Rows - Y positions from top of panel (Y offset from center)
        float rowY   = 155f;  // topmost row y from center
        float rowH   = 36f;
        float step   = 44f;

        MakeOptionsRow(panel.transform, "DisplayMode",  "DISPLAY MODE",
            "FULLSCREEN", new Vector2(0, rowY));            // row 0
        MakeOptionsRow(panel.transform, "Resolution",   "RESOLUTION",
            "1920 x 1080", new Vector2(0, rowY - step));   // row 1
        MakeToggleRow(panel.transform,  "VSync",        "VSYNC",
            "ON",  new Vector2(0, rowY - step*2));          // row 2
        MakeOptionsRow(panel.transform, "MasterVolume", "MASTER VOLUME",
            "80%", new Vector2(0, rowY - step*3));          // row 3
        MakeOptionsRow(panel.transform, "SFXVolume",    "SFX VOLUME",
            "100%",new Vector2(0, rowY - step*4));          // row 4
        MakeOptionsRow(panel.transform, "MusicVolume",  "MUSIC VOLUME",
            "60%", new Vector2(0, rowY - step*5));          // row 5
        MakeToggleRow(panel.transform,  "ShowFPS",      "SHOW FPS",
            "OFF", new Vector2(0, rowY - step*6));          // row 6

        // Back button
        MakeMenuButton(panel.transform, "OptionsBackBtn",
            "< BACK TO MENU", BTN_BG, BTN_BG, BTN_BD, new Color(.5f,.48f,.55f),
            new Vector2(0, -195), new Vector2(240,34));

        return panel;
    }

    static void MakeOptionsRow(Transform parent, string id, string label, string defVal, Vector2 pos)
    {
        GameObject row = MakeGO(id + "Row", parent);
        SetRT(row, V(.5f,.5f), V(.5f,.5f), pos, new Vector2(500,36));

        MakeText(row.transform, "Label", label, 17, OPT_LBL,
            V(0,.5f), V(0,.5f), new Vector2(14,0), new Vector2(200,30), TextAnchor.MiddleLeft);
        MakeMenuButton(row.transform, id+"LeftBtn", "<", BTN_BG, BTN_BG, BTN_BD, OPT_VAL,
            new Vector2(228,0), new Vector2(28,28));
        MakeText(row.transform, id+"Value", defVal, 17, OPT_VAL,
            V(.5f,.5f), V(.5f,.5f), new Vector2(16,0), new Vector2(110,30), TextAnchor.MiddleCenter);
        MakeMenuButton(row.transform, id+"RightBtn", ">", BTN_BG, BTN_BG, BTN_BD, OPT_VAL,
            new Vector2(292,0), new Vector2(28,28));

        // Separator
        GameObject sep = MakeGO("Sep", row.transform);
        SetRT(sep, V(0,0), V(1,0), new Vector2(0,0), new Vector2(0,1));
        MakeImageOnGO(sep, new Color(.2f,.18f,.28f,.5f));
    }

    static void MakeToggleRow(Transform parent, string id, string label, string defVal, Vector2 pos)
    {
        GameObject row = MakeGO(id + "Row", parent);
        SetRT(row, V(.5f,.5f), V(.5f,.5f), pos, new Vector2(500,36));

        MakeText(row.transform, "Label", label, 17, OPT_LBL,
            V(0,.5f), V(0,.5f), new Vector2(14,0), new Vector2(200,30), TextAnchor.MiddleLeft);
        MakeMenuButton(row.transform, id+"ToggleBtn", defVal, BTN_BG, BTN_BG, BTN_BD, OPT_VAL,
            new Vector2(258,0), new Vector2(80,28));

        GameObject sep = MakeGO("Sep", row.transform);
        SetRT(sep, V(0,0), V(1,0), new Vector2(0,0), new Vector2(0,1));
        MakeImageOnGO(sep, new Color(.2f,.18f,.28f,.5f));
    }

    static void WireOptionsPanel(GameObject optPanel, MainMenuUI ui)
    {
        ui.displayModeValue  = FindChildText(optPanel, "DisplayModeValue");
        ui.displayModeLeft   = FindChildBtn(optPanel,  "DisplayModeLeftBtn");
        ui.displayModeRight  = FindChildBtn(optPanel,  "DisplayModeRightBtn");
        ui.resolutionValue   = FindChildText(optPanel, "ResolutionValue");
        ui.resolutionLeft    = FindChildBtn(optPanel,  "ResolutionLeftBtn");
        ui.resolutionRight   = FindChildBtn(optPanel,  "ResolutionRightBtn");
        ui.masterVolumeValue = FindChildText(optPanel, "MasterVolumeValue");
        ui.masterVolumeLeft  = FindChildBtn(optPanel,  "MasterVolumeLeftBtn");
        ui.masterVolumeRight = FindChildBtn(optPanel,  "MasterVolumeRightBtn");
        ui.sfxVolumeValue    = FindChildText(optPanel, "SFXVolumeValue");
        ui.sfxVolumeLeft     = FindChildBtn(optPanel,  "SFXVolumeLeftBtn");
        ui.sfxVolumeRight    = FindChildBtn(optPanel,  "SFXVolumeRightBtn");
        ui.musicVolumeValue  = FindChildText(optPanel, "MusicVolumeValue");
        ui.musicVolumeLeft   = FindChildBtn(optPanel,  "MusicVolumeLeftBtn");
        ui.musicVolumeRight  = FindChildBtn(optPanel,  "MusicVolumeRightBtn");
        ui.vsyncValueText    = FindChildText(optPanel, "VSyncToggleBtn");
        ui.vsyncToggleBtn    = FindChildBtn(optPanel,  "VSyncToggleBtn");
        ui.showFPSValueText  = FindChildText(optPanel, "ShowFPSToggleBtn");
        ui.showFPSToggleBtn  = FindChildBtn(optPanel,  "ShowFPSToggleBtn");
        ui.optionsBackBtn    = FindChildBtn(optPanel,  "OptionsBackBtn");
    }

    // ================================================================== 2. EYE CALIBRATION
    static void BuildEyeCalibrationScene(Sprite sq)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureEventSystem();

        GameObject camGO = new GameObject("MainCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG;
        cam.orthographic = true;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        var (canvas, _) = MakeCanvas();
        MakeImage(canvas, "BG", BG, V(0,0), V(1,1), Vector2.zero, Vector2.zero, 0);
        AddCorners(canvas, CORNER, 0);
        AddTorch(canvas, "TorchTL", FLAME, TORCH, V(0,1), V(0,1), new Vector2( 28,-28));
        AddTorch(canvas, "TorchTR", FLAME, TORCH, V(1,1), V(1,1), new Vector2(-28,-28));

        // Center panel
        GameObject panel = MakeGO("CalPanel", canvas);
        SetRT(panel, V(.5f,.5f), V(.5f,.5f), Vector2.zero, new Vector2(480,440));
        MakeImageOnGO(panel, PANEL_BG);

        MakeText(panel.transform, "CalTitle",
            "EYE GAZING CONFIGURATION", 22, TEAL_TX,
            V(.5f,1f), V(.5f,1f), new Vector2(0,-36), new Vector2(440,32));

        // Webcam frame
        GameObject camFrame = MakeGO("WebcamFrame", panel.transform);
        SetRT(camFrame, V(.5f,1f), V(.5f,1f), new Vector2(0,-120), new Vector2(240,135));
        RawImage rawImg = camFrame.AddComponent<RawImage>();
        rawImg.color = new Color(.02f,.04f,.08f);

        // Face guide circles (decorative)
        GameObject guide = MakeGO("FaceGuide", camFrame.transform);
        SetRT(guide, V(.5f,.5f), V(.5f,.5f), Vector2.zero, new Vector2(80,96));
        Image guideImg = guide.AddComponent<Image>();
        guideImg.color = new Color(.16f,.24f,.36f,.5f);

        // Steps
        MakeText(panel.transform, "Step1",
            "1.  Center your face in the frame above\n    and look directly at the camera.",
            16, OPT_LBL, V(.5f,.5f), V(.5f,.5f), new Vector2(0, 50), new Vector2(420, 60),
            TextAnchor.UpperLeft);
        MakeText(panel.transform, "Step2",
            "2.  Adjust sensitivity so small head\n    movements cover the whole screen.",
            16, OPT_LBL, V(.5f,.5f), V(.5f,.5f), new Vector2(0, -15), new Vector2(420, 60),
            TextAnchor.UpperLeft);

        // Sensitivity label + value
        MakeText(panel.transform, "SensLabel", "SENSITIVITY", 16, OPT_LBL,
            V(.5f,.5f), V(.5f,.5f), new Vector2(-100,-78), new Vector2(180,28), TextAnchor.MiddleLeft);
        Text sensVal = MakeText(panel.transform, "SensValue", "65%", 16, OPT_VAL,
            V(.5f,.5f), V(.5f,.5f), new Vector2(130,-78), new Vector2(60,28), TextAnchor.MiddleRight);

        // Sensitivity slider (simple, no handle)
        GameObject sliderGO = MakeGO("SensSlider", panel.transform);
        SetRT(sliderGO, V(.5f,.5f), V(.5f,.5f), new Vector2(10,-78), new Vector2(160,10));
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.65f;
        GameObject slBG = MakeGO("Background", sliderGO.transform);
        SetRT(slBG, V(0,0), V(1,1), Vector2.zero, Vector2.zero);
        MakeImageOnGO(slBG, H("#1A1030"));
        GameObject fillArea = MakeGO("FillArea", sliderGO.transform);
        SetRT(fillArea, V(0,0), V(1,1), Vector2.zero, Vector2.zero);
        GameObject fill = MakeGO("Fill", fillArea.transform);
        RectTransform fillRT = fill.GetComponent<RectTransform>() ?? fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = new Vector2(0.65f,1f);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        MakeImageOnGO(fill, H("#2A6A80"));
        slider.fillRect = fillRT;
        slider.targetGraphic = slBG.GetComponent<Image>();

        // Status text
        Text statusTxt = MakeText(panel.transform, "StatusText", "WEBCAM DETECTED - READY",
            14, H("#20A060"), V(.5f,.5f), V(.5f,.5f), new Vector2(0,-116), new Vector2(420,22));

        // Buttons
        Button calBtn  = MakeMenuButton(panel.transform, "CalibrateBtn",
            "CALIBRATE AND RETURN >", BTN_BG, H("#081518"), TEAL_BD, TEAL_TX,
            new Vector2(0,-158), new Vector2(340,44));
        Button backBtn = MakeMenuButton(panel.transform, "BackBtn",
            "< BACK TO MENU", BTN_BG, BTN_BG, BTN_BD, new Color(.5f,.48f,.55f),
            new Vector2(0,-198), new Vector2(240,34));

        // Manager
        GameObject mgr = new GameObject("CalibrationManager");
        EyeCalibrationUI calUI = mgr.AddComponent<EyeCalibrationUI>();
        calUI.webcamPreview      = rawImg;
        calUI.sensitivitySlider  = slider;
        calUI.sensitivityValueText = sensVal;
        calUI.statusText         = statusTxt;
        calUI.calibrateBtn       = calBtn;
        calUI.backBtn            = backBtn;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/EyeCalibration.unity");
    }

    // ================================================================== 3. DEMO LEVEL
    static void BuildDemoLevelScene(Sprite sq, int wallLayer)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureEventSystem();

        int rows = Maze.Length, cols = Maze[0].Length;

        // Camera
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = Color.black;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();
        cam.transform.position = new Vector3((cols * CellSize) / 2f,
                                             (rows * CellSize) / 2f, -10f);
        cam.orthographicSize   = (rows * CellSize) / 2f * 1.15f;

        // Lights
        GameObject globalLightGO = new GameObject("GlobalLight2D");
        TryAddLight2D(globalLightGO, "Global", 0.04f, Color.white);
        GameObject visionLightGO = new GameObject("VisionLight");
        TryAddLight2D(visionLightGO, "Point", 1.6f, new Color(1f, 0.95f, 0.8f), 2.4f);

        // Maze
        GameObject mazeRoot = new GameObject("Maze");
        Transform player = null;
        System.Collections.Generic.List<Vector3> enemySpawns = new System.Collections.Generic.List<Vector3>();
        Vector3 exitPos = Vector3.zero;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            char ch = Maze[r][c];
            Vector3 pos = new Vector3(c * CellSize, (rows - 1 - r) * CellSize, 0f);

            if (ch == '#')
            { CreateWorldTile(mazeRoot.transform, pos, sq, wallLayer, new Color(.22f,.22f,.28f), 1, true);  continue; }

            CreateWorldTile(mazeRoot.transform, pos, sq, -1, new Color(.45f,.45f,.5f), 0, false);

            if (ch == 'S') player  = CreatePlayer(pos, sq);
            else if (ch == 'E') exitPos = pos;
            else if (ch == 'X') enemySpawns.Add(pos);
        }

        // Exit
        GameObject exitGO = new GameObject("Exit");
        exitGO.transform.position = exitPos;
        exitGO.transform.localScale = Vector3.one * CellSize * 0.8f;
        exitGO.AddComponent<SpriteRenderer>().sprite = sq;
        exitGO.GetComponent<SpriteRenderer>().color = new Color(.2f,1f,.4f,.65f);
        exitGO.GetComponent<SpriteRenderer>().sortingOrder = 2;
        BoxCollider2D bc = exitGO.AddComponent<BoxCollider2D>(); bc.isTrigger = true;

        // Enemies
        LayerMask wallMask = 1 << wallLayer;
        foreach (Vector3 sp in enemySpawns)
        {
            GameObject eGO = new GameObject("Enemy");
            eGO.transform.position = sp;
            eGO.transform.localScale = Vector3.one * CellSize * 0.7f;
            SpriteRenderer sr = eGO.AddComponent<SpriteRenderer>();
            sr.sprite = sq; sr.color = new Color(.9f,.15f,.2f); sr.sortingOrder = 4;
            Rigidbody2D rb = eGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true;
            eGO.AddComponent<CircleCollider2D>();
            EnemyAI ai = eGO.AddComponent<EnemyAI>();
            ai.player = player; ai.vision = null; ai.wallLayer = wallMask; // vision wired below
        }

        // Managers
        GameObject mgrs = new GameObject("Managers");
        GameManager  gm  = mgrs.AddComponent<GameManager>();
        GazeTracker  gt  = mgrs.AddComponent<GazeTracker>();
        FlashlightController vc = mgrs.AddComponent<FlashlightController>();
        SanityManager sm  = mgrs.AddComponent<SanityManager>();

        vc.gazeTracker  = gt;
        vc.mainCamera   = cam;
        vc.visionLight  = visionLightGO.transform;
        vc.visionRadius = 2.2f;

        sm.player = player;
        sm.vision = vc;

        gm.gazeTracker   = gt;
        gm.sanityManager = sm;

        foreach (EnemyAI ai in UnityEngine.Object.FindObjectsOfType<EnemyAI>())
            ai.vision = vc;

        // UI (canvas)
        var (canvas, _) = MakeCanvas();
        // Sanity slider
        GameObject sliderGO = MakeGO("SanitySlider", canvas);
        SetRT(sliderGO, V(0,1), V(0,1), new Vector2(170,-30), new Vector2(300,18));
        Slider sanSlider = sliderGO.AddComponent<Slider>();
        sanSlider.minValue = 0; sanSlider.maxValue = 1; sanSlider.value = 1;
        sanSlider.interactable = false;
        GameObject slBG = MakeGO("BG", sliderGO.transform);
        SetRT(slBG, V(0,0), V(1,1), Vector2.zero, Vector2.zero);
        MakeImageOnGO(slBG, new Color(.15f,.15f,.15f));
        GameObject fillArea = MakeGO("FillArea", sliderGO.transform);
        SetRT(fillArea, V(0,0), V(1,1), Vector2.zero, Vector2.zero);
        GameObject fill = MakeGO("Fill", fillArea.transform);
        RectTransform fillRT = fill.GetComponent<RectTransform>() ?? fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>(); fillImg.color = new Color(.8f,.2f,.3f);
        sanSlider.fillRect = fillRT;
        sanSlider.targetGraphic = slBG.GetComponent<Image>();
        MakeText(canvas, "SanityLabel", "SANITY", 14, OPT_LBL,
            V(0,1), V(0,1), new Vector2(40,-30), new Vector2(60,18), TextAnchor.MiddleLeft);
        sm.sanitySlider = sanSlider;

        // Intro panel
        GameObject introPanel = MakeGO("IntroPanel", canvas);
        SetRT(introPanel, V(0,0), V(1,1), Vector2.zero, Vector2.zero);
        MakeImageOnGO(introPanel, new Color(0,0,0,.78f));
        MakeText(introPanel.transform, "IntroText",
            "Look at the webcam and press SPACE to calibrate.\n\nPress SPACE to start.",
            28, GOLD1, V(.5f,.5f), V(.5f,.5f), Vector2.zero, new Vector2(700,120));

        gm.introPanel = introPanel;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/DemoLevel.unity");
    }

    // ================================================================== 4. GAME OVER
    static void BuildGameOverScene(Sprite sq)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureEventSystem();

        GameObject camGO = new GameObject("MainCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RED_BG;
        cam.orthographic = true;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        var (canvas, _) = MakeCanvas();
        MakeImage(canvas, "BG", RED_BG, V(0,0), V(1,1), Vector2.zero, Vector2.zero, 0);
        AddCorners(canvas, H("#2A1010"), 0);
        AddTorch(canvas, "TorchTL", H("#801010"), H("#300808"), V(0,1), V(0,1), new Vector2( 28,-28));
        AddTorch(canvas, "TorchTR", H("#801010"), H("#300808"), V(1,1), V(1,1), new Vector2(-28,-28));

        // Skull deco
        MakeText(canvas, "SkullDeco", "X", 64, new Color(.3f,.05f,.05f,.7f),
            V(.5f,.75f), V(.5f,.75f), Vector2.zero, new Vector2(80,80));

        // Cause title (filled at runtime by GameOverUI)
        Text causeTxt = MakeText(canvas, "CauseTitle", "SANITY LOST", 72, RED_DRK,
            V(.5f,.65f), V(.5f,.65f), Vector2.zero, new Vector2(800,90), TextAnchor.MiddleCenter);
        causeTxt.fontStyle = FontStyle.Bold;

        MakeText(canvas, "SubTitle", "THE DARKNESS CONSUMED YOU", 20, H("#5A2828"),
            V(.5f,.56f), V(.5f,.56f), Vector2.zero, new Vector2(700,32));

        // Stats row
        Text sanTxt  = MakeText(canvas,"SanityStat",  "0%",  30, RED_MED, V(.35f,.46f), V(.35f,.46f), Vector2.zero, new Vector2(120,40));
        MakeText(canvas, "SanityLbl", "SANITY",  12, H("#5A3030"), V(.35f,.46f), V(.35f,.46f), new Vector2(0,-24), new Vector2(120,20));
        Text timeTxt = MakeText(canvas,"TimeStat",    "0:00",30, RED_MED, V(.5f,.46f), V(.5f,.46f), Vector2.zero, new Vector2(120,40));
        MakeText(canvas, "TimeLbl",   "SURVIVED",12, H("#5A3030"), V(.5f,.46f), V(.5f,.46f), new Vector2(0,-24), new Vector2(120,20));
        Text enmTxt  = MakeText(canvas,"EnemyStat",   "0",   30, RED_MED, V(.65f,.46f), V(.65f,.46f), Vector2.zero, new Vector2(120,40));
        MakeText(canvas, "EnemyLbl",  "KILLED",  12, H("#5A3030"), V(.65f,.46f), V(.65f,.46f), new Vector2(0,-24), new Vector2(120,20));

        // Buttons
        Button retryBtn = MakeMenuButton(canvas, "RetryBtn",
            "> TRY AGAIN", H("#0F0808"), H("#1A0505"), H("#5A1010"), H("#E08080"),
            new Vector2(0,-110), new Vector2(300,50));
        Button menuBtn  = MakeMenuButton(canvas, "MenuBtn",
            "< MAIN MENU", H("#0F0808"), H("#0F0808"), H("#3A1010"), H("#803030"),
            new Vector2(0,-168), new Vector2(300,50));

        GameObject mgr = new GameObject("GOManager");
        GameOverUI goUI    = mgr.AddComponent<GameOverUI>();
        goUI.causeText    = causeTxt;
        goUI.sanityText   = sanTxt;
        goUI.timeText     = timeTxt;
        goUI.enemiesText  = enmTxt;
        goUI.retryBtn     = retryBtn;
        goUI.mainMenuBtn  = menuBtn;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameOver.unity");
    }

    // ================================================================== 5. WIN
    static void BuildWinScene(Sprite sq)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EnsureEventSystem();

        GameObject camGO = new GameObject("MainCamera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = AMB_BG;
        cam.orthographic = true;
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();

        var (canvas, _) = MakeCanvas();
        MakeImage(canvas, "BG", AMB_BG, V(0,0), V(1,1), Vector2.zero, Vector2.zero, 0);
        AddCorners(canvas, H("#2A1E04"), 0);
        AddTorch(canvas, "TorchTL", H("#F0C040"), H("#806020"), V(0,1), V(0,1), new Vector2( 28,-28));
        AddTorch(canvas, "TorchTR", H("#F0C040"), H("#806020"), V(1,1), V(1,1), new Vector2(-28,-28));
        AddTorch(canvas, "TorchBL", H("#F0C040"), H("#806020"), V(0,0), V(0,0), new Vector2( 28, 28));
        AddTorch(canvas, "TorchBR", H("#F0C040"), H("#806020"), V(1,0), V(1,0), new Vector2(-28, 28));

        // Rune decos
        MakeText(canvas, "RuneL", "?", 90, new Color(.12f,.10f,.02f,.8f),
            V(.15f,.5f), V(.15f,.5f), Vector2.zero, new Vector2(100,100));
        MakeText(canvas, "RuneR", "?", 90, new Color(.12f,.10f,.02f,.8f),
            V(.85f,.5f), V(.85f,.5f), Vector2.zero, new Vector2(100,100));

        Text titleTxt = MakeText(canvas, "WinTitle", "YOU ESCAPED", 64, AMB_TTL,
            V(.5f,.67f), V(.5f,.67f), Vector2.zero, new Vector2(800,82));
        titleTxt.fontStyle = FontStyle.Bold;
        MakeText(canvas, "SubTitle", "THE LABYRINTH IS BEHIND YOU", 20, H("#80601A"),
            V(.5f,.58f), V(.5f,.58f), Vector2.zero, new Vector2(700,30));

        // Sanity bar
        MakeText(canvas, "SanityBarLbl", "FINAL SANITY", 14, H("#6A5020"),
            V(.5f,.5f), V(.5f,.5f), new Vector2(0, 38), new Vector2(200,20));
        GameObject barOuter = MakeGO("SanityBarOuter", canvas);
        SetRT(barOuter, V(.5f,.5f), V(.5f,.5f), new Vector2(0,18), new Vector2(220,12));
        MakeImageOnGO(barOuter, H("#1A1010"));
        GameObject barFill = MakeGO("SanityBarFill", barOuter.transform);
        RectTransform bfRT = barFill.GetComponent<RectTransform>() ?? barFill.AddComponent<RectTransform>();
        bfRT.anchorMin = Vector2.zero; bfRT.anchorMax = new Vector2(0.62f,1f);
        bfRT.offsetMin = bfRT.offsetMax = Vector2.zero;
        Image barFillImg = barFill.AddComponent<Image>(); barFillImg.color = H("#C8901A");
        barFillImg.type = Image.Type.Filled; barFillImg.fillMethod = Image.FillMethod.Horizontal;

        // Stats
        Text sanTxt  = MakeText(canvas,"SanityStat",  "62%",  30, AMB_STA, V(.35f,.40f), V(.35f,.40f), Vector2.zero, new Vector2(120,40));
        MakeText(canvas,"SanityLbl",  "SANITY", 12, H("#6A5020"), V(.35f,.40f), V(.35f,.40f), new Vector2(0,-24), new Vector2(120,20));
        Text timeTxt = MakeText(canvas,"TimeStat",    "0:00", 30, AMB_STA, V(.5f,.40f),  V(.5f,.40f),  Vector2.zero, new Vector2(120,40));
        MakeText(canvas,"TimeLbl",    "TIME",   12, H("#6A5020"), V(.5f,.40f),  V(.5f,.40f),  new Vector2(0,-24), new Vector2(120,20));
        Text enmTxt  = MakeText(canvas,"EnemyStat",   "0 / 4",30, AMB_STA, V(.65f,.40f), V(.65f,.40f), Vector2.zero, new Vector2(120,40));
        MakeText(canvas,"EnemyLbl",   "ENEMIES",12, H("#6A5020"), V(.65f,.40f), V(.65f,.40f), new Vector2(0,-24), new Vector2(120,20));

        Button playBtn = MakeMenuButton(canvas, "PlayAgainBtn",
            "> PLAY AGAIN", H("#0F0C04"), H("#1A1508"), H("#4A3810"), H("#F0B830"),
            new Vector2(0,-110), new Vector2(300,50));
        Button menuBtn = MakeMenuButton(canvas, "MenuBtn",
            "< MAIN MENU", H("#0F0C04"), H("#0F0C04"), H("#2A2006"), H("#9A7010"),
            new Vector2(0,-168), new Vector2(300,50));

        GameObject mgr = new GameObject("WinManager");
        WinUI winUI         = mgr.AddComponent<WinUI>();
        winUI.sanityText    = sanTxt;
        winUI.timeText      = timeTxt;
        winUI.enemiesText   = enmTxt;
        winUI.sanityBarFill = barFillImg;
        winUI.playAgainBtn  = playBtn;
        winUI.mainMenuBtn   = menuBtn;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Win.unity");
    }

    // ================================================================== BUILD SETTINGS
    static void RegisterBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",       true),
            new EditorBuildSettingsScene("Packages/UnitEye/Scenes/GazeCalibration.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity",      true),
            new EditorBuildSettingsScene("Assets/Scenes/GameOver.unity",       true),
            new EditorBuildSettingsScene("Assets/Scenes/Win.unity",            true),
        };
        Debug.Log("Build Settings updated to use GameScene instead of DemoLevel.");
    }

    // ================================================================== WORLD HELPERS
    static void CreateWorldTile(Transform parent, Vector3 pos, Sprite sprite,
        int layer, Color color, int sortOrder, bool addCollider)
    {
        GameObject go = new GameObject(addCollider ? "Wall" : "Floor");
        go.transform.SetParent(parent);
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * CellSize;
        if (layer >= 0) go.layer = layer;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sortingOrder = sortOrder;
        if (addCollider) go.AddComponent<BoxCollider2D>();
    }

    static Transform CreatePlayer(Vector3 pos, Sprite sprite)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * CellSize * 0.6f;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = new Color(.3f,.6f,1f); sr.sortingOrder = 5;
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; rb.freezeRotation = true;
        go.AddComponent<CircleCollider2D>();
        go.AddComponent<PlayerMovement>();
        return go.transform;
    }

    // ================================================================== UI HELPERS
    static (Transform canvas, RectTransform cRect) MakeCanvas()
    {
        GameObject go = new GameObject("Canvas");
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return (go.transform, go.GetComponent<RectTransform>());
    }

    static Image MakeImage(Transform parent, string name, Color color,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
        int order, Vector2 size = default)
    {
        GameObject go = MakeGO(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        if (size != default) rt.sizeDelta = size;
        Image img = go.AddComponent<Image>(); img.color = color;
        return img;
    }

    static void MakeImageOnGO(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = color;
    }

    static Text MakeText(Transform parent, string name, string content,
        int size, Color color, Vector2 ancMin, Vector2 ancMax, Vector2 ancPos,
        Vector2 sizeDelta, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject go = MakeGO(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = ancPos;
        rt.sizeDelta = sizeDelta;
        Text t = go.AddComponent<Text>();
        t.text = content; t.fontSize = size; t.color = color;
        t.alignment = alignment;
        t.font = GetFont();
        t.resizeTextForBestFit = false;
        return t;
    }

    static Button MakeMenuButton(Transform parent, string name, string label,
        Color bgColor, Color hoverColor, Color borderColor, Color textColor,
        Vector2 anchoredPos, Vector2 size)
    {
        // Outer border image
        GameObject border = MakeGO(name + "_border", parent);
        RectTransform brt = border.GetComponent<RectTransform>() ?? border.AddComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot     = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = anchoredPos;
        brt.sizeDelta = size + new Vector2(2, 2);
        MakeImageOnGO(border, borderColor);

        // Inner button
        GameObject go = MakeGO(name, border.transform);
        RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.one; rt.offsetMax = -Vector2.one;
        Image img = go.AddComponent<Image>(); img.color = bgColor;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = bgColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor     = bgColor * 0.7f;
        cb.selectedColor    = bgColor;
        btn.colors = cb; btn.targetGraphic = img;

        // Label
        GameObject textGO = MakeGO("Label", go.transform);
        RectTransform textRT = textGO.GetComponent<RectTransform>() ?? textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        Text t = textGO.AddComponent<Text>();
        t.text = label; t.fontSize = Mathf.RoundToInt(size.y * 0.42f);
        t.color = textColor; t.alignment = TextAnchor.MiddleCenter;
        t.font = GetFont();

        return btn;
    }

    static void AddCorners(Transform canvas, Color color, int extra)
    {
        float sz = 32f, th = 2f;
        // Top-left
        AddCornerBar(canvas, "CTL_H", color, V(0,1), V(0,1), new Vector2(10+sz/2,-10), new Vector2(sz, th));
        AddCornerBar(canvas, "CTL_V", color, V(0,1), V(0,1), new Vector2(10,    -10-sz/2), new Vector2(th, sz));
        // Top-right
        AddCornerBar(canvas, "CTR_H", color, V(1,1), V(1,1), new Vector2(-10-sz/2,-10), new Vector2(sz, th));
        AddCornerBar(canvas, "CTR_V", color, V(1,1), V(1,1), new Vector2(-10,   -10-sz/2), new Vector2(th, sz));
        // Bottom-left
        AddCornerBar(canvas, "CBL_H", color, V(0,0), V(0,0), new Vector2(10+sz/2, 10), new Vector2(sz, th));
        AddCornerBar(canvas, "CBL_V", color, V(0,0), V(0,0), new Vector2(10,     10+sz/2), new Vector2(th, sz));
        // Bottom-right
        AddCornerBar(canvas, "CBR_H", color, V(1,0), V(1,0), new Vector2(-10-sz/2, 10), new Vector2(sz, th));
        AddCornerBar(canvas, "CBR_V", color, V(1,0), V(1,0), new Vector2(-10,    10+sz/2), new Vector2(th, sz));
    }

    static void AddCornerBar(Transform parent, string name, Color color,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 size)
    {
        GameObject go = MakeGO(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = ancPos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>(); img.color = color;
    }

    static void AddTorch(Transform canvas, string name, Color flameColor, Color bodyColor,
        Vector2 ancMin, Vector2 ancMax, Vector2 ancPos)
    {
        GameObject root = MakeGO(name, canvas);
        RectTransform rt = root.GetComponent<RectTransform>() ?? root.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = ancPos;
        rt.sizeDelta = new Vector2(10, 20);

        // Body (bottom half)
        GameObject body = MakeGO("Body", root.transform);
        RectTransform brt = body.GetComponent<RectTransform>() ?? body.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.2f,0f); brt.anchorMax = new Vector2(0.8f,.5f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        Image bImg = body.AddComponent<Image>(); bImg.color = bodyColor;

        // Flame (top half)
        GameObject flame = MakeGO("Flame", root.transform);
        RectTransform frt = flame.GetComponent<RectTransform>() ?? flame.AddComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f,.5f); frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        Image fImg = flame.AddComponent<Image>(); fImg.color = flameColor;

        TorchFlicker tf = root.AddComponent<TorchFlicker>();
        tf.flameImage   = fImg;
        tf.phaseOffset  = UnityEngine.Random.Range(0f, 3f);
    }

    // ================================================================== MISC HELPERS
    static GameObject MakeGO(string name, Transform parent)
    {
        bool isUIParent = parent is RectTransform || parent.GetComponent<Canvas>() != null;
        GameObject go = isUIParent
            ? new GameObject(name, typeof(RectTransform))
            : new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void SetRT(GameObject go, Vector2 ancMin, Vector2 ancMax,
        Vector2 ancPos, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = ancPos;
        rt.sizeDelta = size;
    }

    static Vector2 V(float x, float y) => new Vector2(x, y);

    static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();

        // Prefer the new Input System UI module when available, fallback to Standalone.
        Type inputSystemModuleType = Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
            go.AddComponent(inputSystemModuleType);
        else
            go.AddComponent<StandaloneInputModule>();
    }

    static Font GetFont()
    {
        Font f = Resources.Load<Font>("Cinzel");
        if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f;
    }

    static Sprite LoadMainMenuBackgroundSprite()
    {
        Sprite bg = Resources.Load<Sprite>("bg");
        if (bg != null) return bg;

        bg = Resources.Load<Sprite>("Background");
        if (bg != null) return bg;

        return AssetDatabase.LoadAssetAtPath<Sprite>("Packages/UnitEye/Resources/bg.png");
    }

    // ================================================================== Light2D via reflection
    static void TryAddLight2D(GameObject go, string typeName, float intensity,
        Color color, float outerRadius = -1f, float falloff = 1f)
    {
        Type t = Type.GetType(
            "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (t == null)
        {
            Debug.LogWarning("Light2D not found for " + go.name + ". " +
                "Add it manually (type=" + typeName + ", intensity=" + intensity + ").");
            return;
        }
        Component l = go.AddComponent(t);
        SetProp(l, "intensity", intensity);
        SetProp(l, "color",     color);
        SetEnumProp(l, "lightType", typeName);
        if (outerRadius >= 0) SetProp(l, "pointLightOuterRadius", outerRadius);
        SetProp(l, "falloffIntensity", falloff);
    }

    static void SetProp(object obj, string prop, object val)
    {
        try { obj.GetType().GetProperty(prop)?.SetValue(obj, val); }
        catch { Debug.LogWarning("Could not set Light2D." + prop + " - set it manually."); }
    }

    static void SetEnumProp(object obj, string prop, string enumVal)
    {
        try
        {
            PropertyInfo pi = obj.GetType().GetProperty(prop);
            if (pi != null) pi.SetValue(obj, Enum.Parse(pi.PropertyType, enumVal));
        }
        catch { Debug.LogWarning("Could not set Light2D." + prop + "=" + enumVal + " - set it manually."); }
    }

    // ================================================================== White square sprite
    static Sprite CreateWhiteSquare()
    {
        const string path = "Assets/Generated/WhiteSquare.png";
        if (!File.Exists(path))
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] px    = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
        }
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType        = TextureImporterType.Sprite;
        imp.spriteImportMode   = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 4;
        imp.filterMode         = FilterMode.Point;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ================================================================== Tags & Layers
    static void EnsureTag(string tag)
    {
        SerializedObject tm = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tm.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tm.ApplyModifiedProperties();
    }

    static int EnsureLayer(string layerName)
    {
        SerializedObject tm = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tm.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            string val = layers.GetArrayElementAtIndex(i).stringValue;
            if (val == layerName) return i;
        }
        for (int i = 8; i < layers.arraySize; i++)
        {
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = layerName;
                tm.ApplyModifiedProperties();
                return i;
            }
        }
        return 0;
    }

    static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // Find first child Text/Button with matching name anywhere in hierarchy
    static Text FindChildText(GameObject root, string name)
    {
        Transform t = FindDeep(root.transform, name);
        return t != null ? t.GetComponentInChildren<Text>() : null;
    }

    static Button FindChildBtn(GameObject root, string name)
    {
        Transform t = FindDeep(root.transform, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}

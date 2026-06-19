using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks elapsed playtime while the gameplay scene is active.
/// </summary>
public static class GameSessionTimer
{
    private static bool _initialized;
    private static bool _running;
    private static float _startRealtime;
    private static float _frozenElapsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_initialized) return;

        _initialized = true;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        HandleScene(SceneManager.GetActiveScene().name);
    }

    private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        HandleScene(newScene.name);
    }

    private static void HandleScene(string sceneName)
    {
        if (sceneName == SceneNames.DemoLevel)
        {
            StartNewRun();
            return;
        }

        if (_running)
            StopAndFreeze();
    }

    public static void StartNewRun()
    {
        _frozenElapsed = 0f;
        _startRealtime = Time.realtimeSinceStartup;
        _running = true;
    }

    public static void StopAndFreeze()
    {
        if (_running)
            _frozenElapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _startRealtime);

        _running = false;
    }

    public static float GetElapsedSeconds()
    {
        if (_running)
            return Mathf.Max(0f, Time.realtimeSinceStartup - _startRealtime);

        return Mathf.Max(0f, _frozenElapsed);
    }

    public static void CopyToResultData()
    {
        GameResultData.SurvivalTime = GetElapsedSeconds();
    }
}
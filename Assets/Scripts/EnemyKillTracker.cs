using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global enemy kill counter that does not depend on a specific manager object.
/// Resets when gameplay scene is loaded.
/// </summary>
public static class EnemyKillTracker
{
    private static bool _initialized;
    private static int _kills;

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
            Reset();
    }

    public static void Reset()
    {
        _kills = 0;
    }

    public static void RegisterKill()
    {
        _kills++;
    }

    public static int GetKills()
    {
        return Mathf.Max(0, _kills);
    }

    public static void CopyToResultData()
    {
        GameResultData.EnemiesKilled = Mathf.Max(GameResultData.EnemiesKilled, GetKills());
    }
}
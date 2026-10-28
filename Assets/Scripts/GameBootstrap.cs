using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Object.FindFirstObjectByType<GameManager>() == null) return;
        if (Object.FindFirstObjectByType<LevelManager>() != null) return;

        var go = new GameObject("GameSystems");
        go.AddComponent<MarketBuilder>();
        go.AddComponent<LevelManager>();
        go.AddComponent<ForkInteraction>();
        go.AddComponent<AudioFX>();
        go.AddComponent<FxSpawner>();
        go.AddComponent<OrbitCamera>();
        AddOptional(go, "RetroLook");
        AddOptional(go, "ForkliftSkin");
    }

    private static void AddOptional(GameObject go, string typeName)
    {
        var t = System.Type.GetType(typeName);
        if (t != null) go.AddComponent(t);
    }
}

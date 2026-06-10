using UnityEngine;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<GameManager>() != null) return;
        var go = new GameObject("GameManager");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameManager>();
    }
}

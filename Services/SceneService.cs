using MelonLoader;
using UnityEngine.SceneManagement;
using Il2CppInterop.Runtime.Attributes;
using Il2Cpp;

namespace LastEpochPandora.Services
{
    public static class SceneService
    {
        private static Action<Scene, LoadSceneMode> sceneLoadedHandler;
        public static string _lastLoadedSceneName = "<None>";
        private static readonly List<Action<string>> sceneChangeCallbacks = new();

        private static readonly string[] _invalidScenes = new[]
        {
            "ClientSplash",
            "Login",
            "CharacterSelect",
            "IntroCutscene",
            "null",
            "<None>",
        };

        public static void Init()
        {
            sceneLoadedHandler = OnSceneLoaded;
            SceneManager.sceneLoaded += sceneLoadedHandler;

            MelonLogger.Msg("SceneService: SceneManager.sceneLoaded hook registered.");
        }

        public static void DeInit()
        {
            SceneManager.sceneLoaded -= sceneLoadedHandler;
        }

        public static bool InBannedScene()
        {
            if (_lastLoadedSceneName == null)
                return true;
            foreach (var scene in _invalidScenes)
            {
                if (_lastLoadedSceneName.Contains(scene))
                {
                    return true;
                }
            }
            return false;
        }

        [HideFromIl2Cpp]
        public static void RegisterSceneChangeCallback(Action<string> callback)
        {
            sceneChangeCallbacks.Add(callback);
        }

        [HideFromIl2Cpp]
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _lastLoadedSceneName = SceneManager.GetActiveScene().name?.ToString() ?? "<Unnamed>";
            MelonLogger.Msg($"Scene Loaded: {_lastLoadedSceneName}");

            if (!InBannedScene())
            {
                foreach (var callback in sceneChangeCallbacks)
                {
                    callback(_lastLoadedSceneName);
                }
            }
        }
    }
}
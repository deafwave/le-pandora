using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Il2CppInterop.Runtime.Attributes;

namespace LastEpochPandora.Services
{
    [RegisterTypeInIl2Cpp]
    internal class ReferenceService : MonoBehaviour
    {
        public ReferenceService(IntPtr ptr) : base(ptr) { }
        public static ReferenceService Instance { get; private set; }
        private static Action onLocalPlayerAvailable;
        public static LocalPlayer LocalPlayer { get; private set; }
        private static bool _isWaitingForPlayer = false;
        private static Actor _cachedPlayerActor;


        private float _updateTimer;
        private const float UpdateInterval = 1.0f;

        void Awake()
        {
            Instance = this;
            MelonLogger.Msg("ReferenceService: Initialized.");

        }

        void Update()
        {
            MelonLogger.Msg("ReferenceService: Update called. Current scene: " + SceneManager.GetActiveScene().name);
            if (SceneService.InBannedScene()) return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer < UpdateInterval) return;
            _updateTimer = 0;

            var newLocalPlayer = PlayerFinder.getLocalPlayerInMultiplayer();
            MelonLogger.Msg($"ReferenceService: PlayerFinder result is null? {newLocalPlayer == null}");
            MelonLogger.Msg($"ReferenceService: PlayerFinder.getLocalPlayerInMultiplayer() returned: {newLocalPlayer}");
            MelonLogger.Msg($"ReferenceService: Current LocalPlayer: {LocalPlayer}");

            if (newLocalPlayer != LocalPlayer)
            {
                MelonLogger.Msg("ReferenceService: newLocalPlayer != LocalPlayer is TRUE");
                if (!newLocalPlayer.IsNullOrDestroyed())
                {
                    LocalPlayer = newLocalPlayer;
                    MelonLogger.Msg($"ReferenceService: LocalPlayer updated - {LocalPlayer}");
                    onLocalPlayerAvailable?.Invoke();
                    onLocalPlayerAvailable = null;
                }
                else
                {
                    MelonLogger.Warning("ReferenceService: PlayerFinder returned a destroyed LocalPlayer.");
                    LocalPlayer = null; 
                }
            }
            else
            {
                MelonLogger.Msg("ReferenceService: newLocalPlayer == LocalPlayer is TRUE");
            }
        }

        public static Actor GetPlayerAsActor()
        {
            if (_cachedPlayerActor != null && !_cachedPlayerActor.IsNullOrDestroyed())
                return _cachedPlayerActor;

            MelonLogger.Warning("[ReferenceService] Player Actor not yet resolved.");
            return null;
        }

        [HideFromIl2Cpp]
        public static void StartResolvingActor()
        {
            if (_cachedPlayerActor == null)
            {
                MelonLogger.Msg("[ReferenceService] Starting coroutine to resolve player Actor...");
                MelonCoroutines.Start(ResolvePlayerActorFromScene());
            }
        }

        [HideFromIl2Cpp]
        private static IEnumerator ResolvePlayerActorFromScene()
        {
            MelonLogger.Msg("[ReferenceService] >>> Starting ResolvePlayerActorFromScene() coroutine...");

            yield return new WaitForSeconds(2f);

            while (true)
            {
                MelonLogger.Msg("[ReferenceService] Starting deep scan for Actor on any GameObject...");

                int actorHits = 0;
                var allObjects = GameObject.FindObjectsOfType<GameObject>();

                foreach (var go in allObjects)
                {
                    if (go.IsNullOrDestroyed()) continue;

                    var actor = go.GetComponent<Actor>();
                    if (actor != null && !actor.IsNullOrDestroyed())
                    {
                        actorHits++;
                        var ph = go.GetComponent<PlayerHealth>();
                        var uh = go.GetComponent<UnitHealth>();

                        MelonLogger.Msg($"[ActorScan] {go.name} | Actor ✅ | PlayerHealth: {ph != null} | UnitHealth: {uh != null}");
                        if (ph != null && !ph.IsNullOrDestroyed())
                        {
                            _cachedPlayerActor = actor;
                            MelonLogger.Msg($"[ReferenceService] ✅ Actor resolved via PlayerHealth match: {actor.name}");
                            yield break;
                        }
                    }
                }

                MelonLogger.Msg($"[ReferenceService] Deep scan complete. Found {actorHits} Actors.");

                var allPH = GameObject.FindObjectsOfType<PlayerHealth>();
                foreach (var ph in allPH)
                {
                    if (ph.IsNullOrDestroyed()) continue;

                    var go = ph.gameObject;
                    var actor = go.GetComponent<Actor>();

                    MelonLogger.Msg($"[PlayerHealthScan] {go.name} | Actor: {(actor != null ? actor.name : "null")}");

                    if (actor != null && !actor.IsNullOrDestroyed())
                    {
                        _cachedPlayerActor = actor;
                        MelonLogger.Msg($"[ReferenceService] ✅ Actor resolved from PlayerHealth fallback: {actor.name}");
                        yield break;
                    }
                }

                MelonLogger.Msg("[ReferenceService] Actor still not resolved. Retrying in 1s...");
                yield return new WaitForSeconds(1f);
            }
        }


        [HideFromIl2Cpp]
        public static void LogAllMethods(object obj, string label = "Object")
        {
            if (obj == null)
            {
                MelonLogger.Msg($"[LogAllMethods] {label} is null.");
                return;
            }

            var type = obj.GetType();
            MelonLogger.Msg($"[LogAllMethods] Dumping methods for {label} (Type: {type.FullName})");

            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            foreach (var method in methods)
            {
                try
                {
                    MelonLogger.Msg($"[LogAllMethods] Method: {method.Name} | Return Type: {method.ReturnType}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[LogAllMethods] Failed to read method {method.Name}: {ex.Message}");
                }
            }
        }



        public static void RegisterLocalPlayerAvailableCallback(Action callback)
        {
            if (LocalPlayer != null && !LocalPlayer.IsNullOrDestroyed())
            {
                MelonLogger.Msg("ReferenceService: LocalPlayer already available, immediately invoking callback.");
                callback?.Invoke();

            }
            else
            {
                MelonLogger.Msg("ReferenceService: LocalPlayer not ready, queuing callback.");
                onLocalPlayerAvailable += callback;

                if (!_isWaitingForPlayer)
                {
                    _isWaitingForPlayer = true;
                    MelonCoroutines.Start(WaitForLocalPlayer());
                }
            }
        }

        private static IEnumerator WaitForLocalPlayer()
        {
            while (SceneService.InBannedScene() || LocalPlayer == null || LocalPlayer.IsNullOrDestroyed())
            {
                LocalPlayer = PlayerFinder.getLocalPlayerInMultiplayer();
                yield return new WaitForSeconds(1f);
            }


            MelonLogger.Msg($"[ReferenceService] LocalPlayer is now available: {LocalPlayer}");

            _isWaitingForPlayer = false;

            if (onLocalPlayerAvailable != null)
            {
                onLocalPlayerAvailable?.Invoke();
                onLocalPlayerAvailable = null;
            }
            StartResolvingActor();
        }



        public static bool IsGameScene()
        {
            return !SceneService.InBannedScene();
        }

        public static bool CanRun()
        {
            return IsGameScene() &&
                   Instance != null && // Ensure Instance is initialized
                   LocalPlayer != null &&
                   !LocalPlayer.IsNullOrDestroyed();
        }
    }
}

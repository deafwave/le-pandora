using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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
                    LocalPlayer = null; // Ensure we clear the old reference
                }
            }
            else
            {
                MelonLogger.Msg("ReferenceService: newLocalPlayer == LocalPlayer is TRUE");
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
        }



        public static bool IsGameScene()
        {
            return !SceneService.InBannedScene();
        }

        public static bool CanRun()
        {
            return IsGameScene() &&
                   Instance != null && 
                   LocalPlayer != null &&
                   !LocalPlayer.IsNullOrDestroyed();
        }
    }
}
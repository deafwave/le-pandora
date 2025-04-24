using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace LastEpochPandora.Services
{
    [RegisterTypeInIl2Cpp]
    internal class ReferenceService : MonoBehaviour
    {
        public ReferenceService(IntPtr ptr) : base(ptr) { }
        public static ReferenceService Instance { get; private set; }
        private static Action onLocalPlayerAvailable;
        public static Actor LocalPlayer { get; private set; }
        private static bool _isWaitingForLocalPlayer = false;

        void OnEnable()
        {
            if (Instance == null)
            {
                {
                    Instance = this;
                    MelonLogger.Msg("ReferenceService: Instance set in OnEnable");
                }
            }
        }

        void OnUpdate()
        {
            if (SceneService.InBannedScene()) return;
            var newLocalPlayer = PlayerFinder.getLocalActorSync().GameplayActor.GetActor();
            var blessLocalPlayer = PlayerFinder.getLocalPlayerInMultiplayer();

            MelonLogger.Warning($"Local Player: {newLocalPlayer}");
            MelonLogger.Warning($"Bless Player: {blessLocalPlayer}");

            if (newLocalPlayer != LocalPlayer)
            {
                LocalPlayer = newLocalPlayer;
                MelonLogger.Msg($"Local Player: {newLocalPlayer}");
                MelonLogger.Msg($"Local Player 2: {blessLocalPlayer}");
                onLocalPlayerAvailable?.Invoke();
                onLocalPlayerAvailable = null;
            }
            else
            {
                MelonLogger.Warning("ReferenceService: PlayerFinder returned a destroyed player");
                LocalPlayer = null;
            }
        }

        private static readonly object _callbackLock = new object();

        public static void RegisterLocalPlayerAvailableCallback(Action callback)
        {
            lock (_callbackLock)
            {
                if (LocalPlayer != null && !LocalPlayer.IsNullOrDestroyed() && Instance != null)
                {
                    MelonLogger.Msg("ReferenceService: LocalPlayer already exists, invoking callback");
                    callback?.Invoke();
                }
                else
                {
                    MelonLogger.Msg("ReferenceService: LocalPlayer not yet ready, queuing callback");
                    onLocalPlayerAvailable += callback;

                    if (!_isWaitingForLocalPlayer)
                    {
                        _isWaitingForLocalPlayer = true;
                        MelonCoroutines.Start(WaitForLocalPlayer());
                    }
                }
            }
        }

        private static IEnumerator WaitForLocalPlayer()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (Instance == null) continue;
                if (SceneService.InBannedScene()) continue;

                LocalPlayer = PlayerFinder.getLocalActorSync().GameplayActor.GetActor();
                if (LocalPlayer != null && !LocalPlayer.IsNullOrDestroyed())
                {
                    break;
                }
            }

            MelonLogger.Msg($"ReferenceService: LocalPlayer now exists; {LocalPlayer}");
            _isWaitingForLocalPlayer = false;

            lock (_callbackLock)
            {
                onLocalPlayerAvailable?.Invoke();
                onLocalPlayerAvailable = null;
            }
        }

        public static bool CanRun()
        {
            bool isGameScene = !SceneService.InBannedScene();
            bool localPlayerNotNull = LocalPlayer != null;
            bool localPlayerNotDestroyed = !LocalPlayer.IsNullOrDestroyed();

            MelonLogger.Msg($"ReferenceService.CanRun() - isGameScene: {isGameScene}; localPlayerNotNull: {localPlayerNotNull}; localPlayerNotDestroyed: {localPlayerNotDestroyed}");

            return isGameScene && localPlayerNotNull && localPlayerNotDestroyed;

        }
    }
}

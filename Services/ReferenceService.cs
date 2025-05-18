using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using Microsoft.VisualBasic;
using LastEpochPandora.Mods;
using HarmonyLib;

namespace LastEpochPandora.Services
{
    [RegisterTypeInIl2Cpp]
    internal class ReferenceService : MonoBehaviour
    {
        public ReferenceService(IntPtr ptr) : base(ptr) { }
        public static ReferenceService Instance { get; private set; }
        private static Action onLocalPlayerAvailable;
        public static ActorSync LocalPlayer { get; private set; }
        private static bool _isWaitingForLocalPlayer = false;
        private static bool _patched = false;

        void OnDisable() => MelonLogger.Msg("ReferenceService: Disabled!");
        void OnDestroy() => MelonLogger.Msg("ReferenceService: Destroyed!");
        void OnEnable()
        {
            MelonLogger.Msg("ReferenceService: Enabled!");
            if (Instance == null)
            {
                Instance = this;
                MelonLogger.Msg("ReferenceService: Instance set.");
            }
        }

        void Update()
        {
            if (SceneService.InBannedScene()) return;
            var newLocalPlayer = PlayerFinder.getLocalActorSync();
            

            if (newLocalPlayer != LocalPlayer)
            {
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

                if (!_isWaitingForLocalPlayer)
                {
                    _isWaitingForLocalPlayer = true;
                    MelonCoroutines.Start(WaitForLocalPlayer());
                }
            }
        }

        private static IEnumerator WaitForLocalPlayer()
        {
            while (SceneService.InBannedScene() || LocalPlayer == null || LocalPlayer.IsNullOrDestroyed())
            {
                LocalPlayer = PlayerFinder.getLocalActorSync();
                yield return new WaitForSeconds(1f);
            }

            MelonLogger.Msg($"[ReferenceService] LocalPlayer is now available: {LocalPlayer}");
            _isWaitingForLocalPlayer = false;
            if (onLocalPlayerAvailable != null)
            {
                if (!_patched)
                {
                    MelonLogger.Msg("Patching everything");
                    var harmony = new HarmonyLib.Harmony("com.lastepochpandora.referenceservice");
                    harmony.PatchAll();
                    _patched = true;
                }
                onLocalPlayerAvailable?.Invoke();
                onLocalPlayerAvailable = null;
            }
        }

        public static bool CanRun()
        {
            bool isGameScene = !SceneService.InBannedScene();
            bool instanceNotNull = Instance != null;
            bool localPlayerNotNull = LocalPlayer != null;
            bool localPlayerNotDestroyed = !LocalPlayer.IsNullOrDestroyed();


            //MelonLogger.Msg($"ReferenceService.CanRun() - isGameScene: {isGameScene}; Instance: {Instance}; localPlayerNotNull: {localPlayerNotNull}; localPlayerNotDestroyed: {localPlayerNotDestroyed}");

            return isGameScene && localPlayerNotNull && localPlayerNotDestroyed;

        }
    }
}

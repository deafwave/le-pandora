using Il2Cpp;
using MelonLoader;
using HarmonyLib;
using System;
using LastEpochPandora.Services;
using Il2CppLE.Telemetry;

namespace LastEpochPandora.Mods
{
    public class KillCountMod : IMod
    {
        public string Name => "KillCountMod";
        public string Version => "1.0.0";

        private HarmonyLib.Harmony _harmony;
        private const string HarmonyId = "com.deafwave.lastepochpandora.killcountmod";
        private bool _isPatched = false;

        public void Initialize()
        {
            MelonLogger.Msg($"KillCountMod: Initializing {Name} {Version}");
            ReferenceService.RegisterLocalPlayerAvailableCallback(TryApplyPatches);
        }

        public void Deinitialize()
        {
            MelonLogger.Msg($"KillCountMod: Deinitializing {Name}");
            if (_harmony != null && _isPatched)
            {
                var originalMethod = typeof(KilledEnemies).GetMethod("AddKill");
                if (originalMethod != null)
                {
                    _harmony.Unpatch(originalMethod, HarmonyPatchType.All, HarmonyId);
                    MelonLogger.Msg("KillCountMod: Harmony patches removed.");
                }
            }
        }

        private void TryApplyPatches()
        {
            if (!ReferenceService.CanRun())
            {
                MelonLogger.Msg($"{Name}: Conditions not met for patching");
                return;
            }

            if (_isPatched)
            {
                MelonLogger.Msg($"{Name}: Patches already applied");
                return;
            }

            ApplyPatches();
        }

        private void ApplyPatches()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony(HarmonyId);
                var originalMethod = typeof(KilledEnemies).GetMethod("AddKill");
                if (originalMethod == null)
                {
                    MelonLogger.Warning("KillCountMod: Original method AddKill not found in KilledEnemies.");
                    return;
                }

                var postfix = typeof(KilledEnemies_AddKillPatch).GetMethod("Postfix");
                if (postfix == null)
                {
                    MelonLogger.Warning("KillCountMod: Postfix method not found in KilledEnemies_AddKillPatch.");
                    return;
                }

                _harmony.Patch(originalMethod, postfix: new HarmonyMethod(postfix));
                _isPatched = true;
                MelonLogger.Msg("KillCountMod: Harmony patch applied to KilledEnemies.AddKill.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"KillCountMod: Failed to apply Harmony patch to KilledEnemies.AddKill: {ex}");
            }
        }
    }

    public static class KilledEnemies_AddKillPatch
    {
        public static void Postfix(KilledEnemies __instance)
        {
            MelonLogger.Msg($"KilledEnemies.AddKill invoked. Current killCount: {__instance.killCount}");
            int currentKillCount = __instance.killCount;
        }
    }
}
using Il2Cpp;
using HarmonyLib;
using MelonLoader;
using System;
using LastEpochPandora.Services;

namespace LastEpochPandora.Mods
{
    public class MinimapMod : IMod
    {
        public string Name => "MinimapMod";
        public string Version => "1.0.0";

        private HarmonyLib.Harmony _harmony;
        private const string HarmonyId = "com.deafwave.lastepochpandora.minimapmod";
        private bool _isPatched = false;

        public void Initialize()
        {
            MelonLogger.Msg($"MinimapMod: Initializing {Name} {Version}");

            try
            {
                ReferenceService.RegisterLocalPlayerAvailableCallback(() =>
                {
                    MelonLogger.Msg("MinimapMod: LocalPlayer available callback received");
                    TryApplyPatches();
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"MinimapMod: Failed to register callback: {ex}");
            }
        }

        public void Deinitialize()
        {
            MelonLogger.Msg($"MinimapMod: Deinitializing {Name}");
            if (_harmony != null && _isPatched)
            {
                var originalMethod = typeof(MinimapFogOfWar).GetMethod("Initialize");
                if (originalMethod != null)
                {
                    _harmony.Unpatch(originalMethod, HarmonyPatchType.All, HarmonyId);
                    MelonLogger.Msg("MinimapMod: Harmony patches removed.");
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
                var originalMethod = typeof(MinimapFogOfWar).GetMethod("Initialize");
                if (originalMethod == null)
                {
                    MelonLogger.Warning("MinimapMod: Original method Initialize not found in MinimapFogOfWar.");
                    return;
                }

                var prefix = typeof(MinimapFogOfWar_Initialize).GetMethod("Prefix");
                if (prefix == null)
                {
                    MelonLogger.Warning("MinimapMod: Prefix method not found in MinimapFogOfWar_Initialize.");
                    return;
                }

                _harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefix));
                _isPatched = true;
                MelonLogger.Msg("MinimapMod: Harmony patch applied to MinimapFogOfWar.Initialize.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"MinimapMod: Failed to apply Harmony patch to MinimapFogOfWar.Initialize: {ex}");
            }
        }
    }

    public static class MinimapFogOfWar_Initialize
    {
        public static bool Prefix(MinimapFogOfWar __instance, MinimapFogOfWar.QuadScale __0, UnityEngine.Vector3 __1)
        {
            MelonLogger.Msg("Minimap fog of war initialized");
            __instance.discoveryDistance = float.MaxValue;
            return true;
        }
    }
}
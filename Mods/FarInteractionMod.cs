using Il2Cpp;
using HarmonyLib;
using MelonLoader;
using System;
using LastEpochPandora.Services;

namespace LastEpochPandora.Mods
{
    public class FarInteractionsMod : IMod
    {
        public string Name => "FarInteractionsMod";
        public string Version => "1.0.0";

        private HarmonyLib.Harmony _harmony;
        private const string HarmonyId = "com.deafwave.lastepochpandora.farinteractions";
        private bool _isPatched = false;
        public static bool INGAME_IGNORE_NEXT_MOVE = false;

        public void Initialize()
        {
            MelonLogger.Msg($"FarInteractionsMod: Initializing {Name} {Version}");

            ReferenceService.RegisterLocalPlayerAvailableCallback(TryApplyPatches);
        }

        public void Deinitialize()
        {
            MelonLogger.Msg($"FarInteractionMod: Deinitiaizling {Name}");
            if (_harmony != null && _isPatched)
            {
                var originalMethod = typeof(MovingPlayer).GetMethod("MouseClickMoveCommand");
                if (originalMethod != null)
                {
                    _harmony.Unpatch(originalMethod, HarmonyPatchType.All, HarmonyId);
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

                // Patch MovingPlayer.MouseClickMoveCommand
                var movingPlayerMethod = typeof(MovingPlayer).GetMethod("MouseClickMoveCommand");
                if (movingPlayerMethod == null)
                {
                    MelonLogger.Warning($"{Name}: MovingPlayer.MouseClickMoveCommand method not found");
                    return;
                }

                var movingPlayerPrefix = typeof(MovingPlayer_Patch).GetMethod("Prefix");
                if (movingPlayerPrefix == null)
                {
                    MelonLogger.Warning($"{Name}: Prefix method not found in MovingPlayer_Patch");
                    return;
                }

                _harmony.Patch(movingPlayerMethod, prefix: new HarmonyMethod(movingPlayerPrefix));
                MelonLogger.Msg($"{Name}: Patched MovingPlayer.MouseClickMoveCommand");

                // Patch WorldObjectClickListener.ObjectClick
                var worldObjectMethod = typeof(WorldObjectClickListener).GetMethod("ObjectClick");
                if (worldObjectMethod == null)
                {
                    MelonLogger.Warning($"{Name}: WorldObjectClickListener.ObjectClick method not found");
                    return;
                }

                var worldObjectPrefix = typeof(WorldObjectClickListener_Patch).GetMethod("Prefix");
                if (worldObjectPrefix == null)
                {
                    MelonLogger.Warning($"{Name}: Prefix method not found in WorldObjectClickListener_Patch");
                    return;
                }

                _harmony.Patch(worldObjectMethod, prefix: new HarmonyMethod(worldObjectPrefix));
                MelonLogger.Msg($"{Name}: Patched WorldObjectClickListener.ObjectClick");

                _isPatched = true;
                MelonLogger.Msg($"{Name}: All patches applied successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"{Name}: Failed to apply patches: {ex}");
            }
        }

        // Patch implementation classes
        public static class MovingPlayer_Patch
        {
            public static bool Prefix(MovingPlayer __instance)
            {
                if (INGAME_IGNORE_NEXT_MOVE)
                {
                    INGAME_IGNORE_NEXT_MOVE = false;
                    return false;
                }
                return true;
            }
        }

        public static class WorldObjectClickListener_Patch
        {
            public static void Prefix(WorldObjectClickListener __instance)
            {
                __instance.interactionRange = float.MaxValue;
                INGAME_IGNORE_NEXT_MOVE = true;
            }
        }
    }
}
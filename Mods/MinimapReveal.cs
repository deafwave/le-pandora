using Il2Cpp;
using HarmonyLib;
using MelonLoader;

namespace BazaarWebsocketMod.Mods
{
    internal class MinimapReveal
    {
        [HarmonyPatch(typeof(MinimapFogOfWar), "Initialize")]
        public class MinimapFogOfWar_Initialize
        {
            [HarmonyPrefix]
            static bool Prefix(MinimapFogOfWar __instance, MinimapFogOfWar.QuadScale __0, UnityEngine.Vector3 __1)
            {
                MelonLogger.Msg("Minimap fog of war initialized");
                __instance.discoveryDistance = float.MaxValue;
                return true;
            }
        }
    }
}

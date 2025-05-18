using HarmonyLib;
using Il2Cpp;
using Il2CppLE.Factions;
using MelonLoader;

namespace LastEpochPandora.Mods
{
    internal class ChampionLP
    {
        [HarmonyPatch(typeof(TheWeaver))]
        public static class ChanceForLP
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(TheWeaver), nameof(TheWeaver.ChampionsChanceToDropUniquesWithLP), MethodType.Setter)]
            public static bool Prefix_ChampionChanceForLP(ref float value, ref TheWeaver __instance)
            {
                // This doesn't work; I was just testing stuff.
                MelonLogger.Msg(value);
                value = Math.Clamp(value, 500f, 550f);
                return true;
            }
        }
    }
}

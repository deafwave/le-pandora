using HarmonyLib;
using UnityEngine;
using LastEpochPandora.Managers;
using LastEpochPandora.Services;
using Il2CppLE;
using Il2Cpp;

namespace LastEpochPandora.Mods
{
    /*
    [HarmonyPatch(typeof(ShrinePlacementManager))]
    public static class ShrineVacuum
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ShrinePlacementManager.PlaceNewShrine), new Type[] { typeof(GameObject), typeof(Vector3) })]
        public static bool Prefix_PlaceNewShrine_GameObjectVector3(GameObject prefab, ref Vector3 position)
        {
            if (ReferenceService.LocalPlayer != null)
            {
                Vector3 playerPosition = PlayerFinder.getLocalActorSync().transform.position;
                position = playerPosition;
                MelonLoader.MelonLogger.Msg($"[ShrinePatch] Redirecting shrine {prefab.name} to player position: {position}");
            }
            return true;
        }
    } 
    */
}
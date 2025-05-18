using HarmonyLib;
using UnityEngine;
using LastEpochPandora.Managers;
using LastEpochPandora.Services;
using Il2CppLE;
using Il2Cpp;
using MelonLoader;
using Il2CppLE.Networking.Generated.ShrineSync;
using Il2CppLE.Networking.Core;
using Il2CppPixelCrushers.DialogueSystem;
using Il2CppLE.Networking.Generated;
using Il2CppLE.Gameplay.Monolith.Frontend;
using Il2CppLE.Networking.Core.Networking;

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
            }
            return true;
        }
    }
    */

    // This "works"; it creates an instance of a new shrine, but it has no network logic
    // If someone can solve the network request to make a new shrine, it'll work online. 
    [HarmonyPatch(typeof(ShrineSync), "OnInteraction")]
    public class ShrineSync_OnInteraction
    {
        static void Postfix(ref ShrineSync __instance, GameObject entity)
        {
            MelonLogger.Msg("New Shrine Key: " + __instance.Key);
            MelonLogger.Msg("NetObjectKey: " + __instance.NetObject.Key);
            MelonLogger.Msg("NetObjectID: " + __instance.NetObject.Id);
            MelonLogger.Msg("NetObject NetworkID : " + __instance.NetObject.NetworkId);
            Vector3 playerPosition = PlayerFinder.getLocalActorSync().transform.position;
            ShrinePlacementManager shrinePlacementManager = GameObject.FindObjectOfType<ShrinePlacementManager>();
            if (!shrinePlacementManager.IsNullOrDestroyed())
            {
                var shrineID = 0;
                var shrinePrefab = shrinePlacementManager.PlaceNewShrine(shrineID, playerPosition);
                if (shrinePrefab != null)
                {
                    shrinePlacementManager.SetUpShrineVisualsAndNetworking(
                        shrinePrefab,
                        false,
                        shrineID,
                        playerPosition);

                    shrinePrefab.AddComponent<NetBehaviour<ShrineSyncNetObject, MessageFieldDataShrineSync>>();
                }
            }

        }
    }



    /*
    [HarmonyPatch(typeof(WorldObjectClickListener), "ObjectClick")]
    public class WorldObjectClickListener_ObjectClick
    {
        [HarmonyPostfix]
        static void PostFix(ref WorldObjectClickListener __instance, UnityEngine.GameObject __0, bool __1)
        {
            if ((__instance.gameObject.name.ToLower().Contains("shrine")) && (__1 == true))
            {
                GameObject copy = GameObject.Instantiate(__instance.gameObject);
                Vector3 playerPosition = PlayerFinder.getLocalActorSync().transform.position;
                Vector3 position = __instance.gameObject.transform.position;
                ShrinePlacementManager shrine_placement_manager = GameObject.FindObjectOfType<ShrinePlacementManager>();
                if (!shrine_placement_manager.IsNullOrDestroyed()) { shrine_placement_manager.PlaceNewShrine(34, playerPosition); }
            }
        }
    }
    */


}

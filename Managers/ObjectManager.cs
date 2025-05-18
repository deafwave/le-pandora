using Il2Cpp;
using Il2CppNetworking.Multiplayer.Interactables.Portals;
using MelonLoader;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using LastEpochPandora.Services;
using Il2CppLE.Networking.Core;
using HarmonyLib;
using LastEpochPandora.Mods;

namespace LastEpochPandora.Managers
{
    internal static class ObjectManager
    {
        private static LocalPlayer _localPlayer;
        private static ChestPlacementManager _chestPlacementManager;
        private static PlayerSync _playerSync;

        private static List<PortalSync> _portalsCache = new();
        private static List<GameObject> _activeShrinesCache = new();
        private static List<NemesisContainers> _nemesisContainersCache = new();
        private static List<OneShotCacheInteractableObject> _cachesCache = new();
        private static float _lastCacheUpdateTime;
        private const float CACHE_UPDATE_INTERVAL = 1f;

        internal static List<PortalSync> GetPortalsSync()
        {
            UpdateCacheIfNeeded(ref _portalsCache, () => UnityEngine.Object.FindObjectsOfType<PortalSync>().ToList());
            return _portalsCache;
        }

        internal static PlayerSync GetPlayerSync() => _playerSync ??= PlayerSync.local;

        internal static ChestPlacementManager GetChestPlacementManager()
        {
            if (_chestPlacementManager == null && !SceneService.InBannedScene())
                _chestPlacementManager = GameObject.Find("ChestPlacementManager")?.GetComponent<ChestPlacementManager>();
            return _chestPlacementManager;
        }

        internal static List<GameObject> GetShrines()
        {
            UpdateShrineCacheIfNeeded();
            return _activeShrinesCache;
        }

        internal static List<NemesisContainers> GetNemesisEncounters()
        {
            UpdateNemesisCache();
            return _nemesisContainersCache;
        }

        private static void UpdateNemesisCache()
        {

        }

        private static void UpdateShrineCacheIfNeeded()
        {
            //MelonLogger.Msg($"[ShrineCache] Updating shrine cache at Time: {Time.time}");

            if (Time.time - _lastCacheUpdateTime > CACHE_UPDATE_INTERVAL)
            {
                _activeShrinesCache.Clear();
                //MelonLogger.Msg("[ShrineCache] Clearing shrine cache.");

                var allShrineSyncs = UnityEngine.Object.FindObjectsOfType<ShrineSync>();
                //MelonLogger.Msg($"[ShrineCache] Found {allShrineSyncs.Length} ShrineSync objects.");

                foreach (var shrineSync in allShrineSyncs)
                {
                    if (shrineSync != null && shrineSync.gameObject != null && shrineSync.gameObject.activeInHierarchy && shrineSync.ShrineObject != null)
                    {
                        //MelonLogger.Msg($"[ShrineCache] Examining ShrineSync: {shrineSync.gameObject.name}, ShrineObject: {shrineSync.ShrineObject.name}");
                        if (!IsShrineUsed(shrineSync.ShrineObject))
                        {
                            _activeShrinesCache.Add(shrineSync.ShrineObject);
                            //shrineSync.OnInteraction(PlayerFinder.getLocalActorSync().gameObject); // Auto uses shrines upon loading it
                            //MelonLogger.Msg($"[ShrineCache] Added active unused shrine to cache: {shrineSync.ShrineObject.name}");
                        }
                        else
                        {
                            //MelonLogger.Msg($"[ShrineCache] Shrine '{shrineSync.ShrineObject.name}' is considered used.");
                        }
                    }
                }


                _lastCacheUpdateTime = Time.time;
                //MelonLogger.Msg($"[ShrineCache] Cache update complete, active shrine count: {_activeShrinesCache.Count}");
            }
            else
            {
                //MelonLogger.Msg($"[ShrineCache] Cache is still valid (updated at Time: {_lastCacheUpdateTime}).");
            }
        }


        private static bool IsShrineUsed(GameObject shrineObject)
        {
            if (shrineObject == null)
            {
                return true;
            }

            var conditionHandler = shrineObject.GetComponent<ConditionHandler>();
            if (conditionHandler != null)
            {
                //MelonLogger.Msg($"[ShrineCheck] '{shrineObject.name}' ConditionHandler.triggered: {conditionHandler.triggered}");
                if (shrineObject.name.Contains("Lizard")) MelonLogger.Warning("LIZARDS FOUND");
                return conditionHandler.triggered;
            }
            else
            {
                MelonLogger.Warning($"[ShrineCheck] '{shrineObject.name}' has no ConditionHandler component.");
                return shrineObject.name.Contains("Used") || shrineObject.name.Contains("Disabled");
            }
        }


        internal static List<OneShotCacheInteractableObject> GetCaches()
        {
            UpdateCacheIfNeeded(ref _cachesCache, () =>
            {
                return OneShotCacheInteractableObject.all?.ToArray().ToList() ?? new List<OneShotCacheInteractableObject>();
            });
            return _cachesCache;
        }

        private static void UpdateCacheIfNeeded<T>(ref List<T> cache, Func<List<T>> updateFunc)
        {
            if (Time.time - _lastCacheUpdateTime > CACHE_UPDATE_INTERVAL)
            {
                cache = updateFunc();
                _lastCacheUpdateTime = Time.time;
            }
        }

        internal static void OnSceneLoaded()
        {
            _localPlayer = null;
            _chestPlacementManager = null;
            _playerSync = null;
            _activeShrinesCache.Clear();
            _lastCacheUpdateTime = 0f;
            _cachesCache.Clear();
        }

        internal static bool HasPlayer() => GetLocalPlayer() != null;
        internal static LocalPlayer GetLocalPlayer() => _localPlayer ??= LocalPlayer.instance;
    }
}
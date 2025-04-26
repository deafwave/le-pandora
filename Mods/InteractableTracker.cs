using Il2Cpp;
using LastEpochPandora.Managers;
using LastEpochPandora.Services;
using MelonLoader;
using System;
using UnityEngine;

namespace LastEpochPandora.Mods
{
    internal class InteractableTracker
    {
        private static bool isInitialized = false;
        private static float updateInterval = 0.5f;
        private static float lastUpdateTime = 0f;
        private static GameObject updaterObject;
        private static bool isProcessing = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            MelonLogger.Msg("InteractableTracker: Initializing");

            SceneService.RegisterSceneChangeCallback(scene =>
            {
                if (!SceneService.InBannedScene())
                {
                    MelonLogger.Msg($"InteractableTracker: Scene changed to {scene}, will update interactables on next frame");
                    ForceUpdate();
                }
            });

            ReferenceService.RegisterLocalPlayerAvailableCallback(() =>
            {
                if (!isInitialized)
                {
                    CreateUpdater();
                    isInitialized = true;
                    ForceUpdate();
                }
            });

            if (ReferenceService.CanRun())
            {
                CreateUpdater();
                isInitialized = true;
                ForceUpdate();
            }
        }

        private static void CreateUpdater()
        {
            if (updaterObject == null)
            {
                updaterObject = new GameObject("Pandora_InteractableUpdater");
                updaterObject.hideFlags = HideFlags.HideAndDontSave;
                GameObject.DontDestroyOnLoad(updaterObject);
                updaterObject.AddComponent<InteractableTrackerUpdater>();
            }

            MelonLogger.Msg("InteractableTracker: Updater created");
        }

        public static void Update()
        {
            if (!isInitialized || !ReferenceService.CanRun() || isProcessing)
                return;

            if (Time.time - lastUpdateTime < updateInterval)
                return;

            lastUpdateTime = Time.time;
            isProcessing = true;

            try
            {
                ProcessInteractables();
            }
            finally
            {
                isProcessing = false;
            }
        }

        private static void ProcessInteractables()
        {
            try
            {
                ESPService.ClearElements();

                var localActor = PlayerFinder.getLocalActorSync();
                if (localActor == null || localActor.transform == null)
                    return;

                Vector3 playerPos = localActor.transform.position;
                int validCount = 0;
                float maxRenderDistance = 100f;

                var shrines = ObjectManager.GetShrines();
                if (shrines != null)
                {
                    foreach (var obj in shrines)
                    {
                        if (obj == null || !obj.activeInHierarchy)
                            continue;

                        Vector3 objPos = obj.transform.position;
                        float distance = Vector3.Distance(playerPos, objPos);

                        if (distance > maxRenderDistance)
                            continue;

                        validCount++;
                        string label = obj.name.Replace("(Clone)", "").Trim();

                        ESPService.QueueLine(playerPos, objPos, Color.yellow);
                        ESPService.QueueLabel(objPos + Vector3.up * 1.5f, label, Color.white);

                        if (distance > 10f)
                        {
                            ESPService.QueueDistanceLabel(playerPos, objPos, Color.magenta, useMidpoint: true);
                        }
                    }
                }
                MelonLogger.Msg($"InteractableTracker: Rendered {validCount} shrines");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"InteractableTracker: Exception while processing interactables: {ex}");
            }
        }

        public static void ForceUpdate()
        {
            lastUpdateTime = 0f;
        }
    }

    [RegisterTypeInIl2Cpp]
    internal class InteractableTrackerUpdater : MonoBehaviour
    {
        public InteractableTrackerUpdater(IntPtr ptr) : base(ptr) { }

        void Update()
        {
            InteractableTracker.Update();
        }

        void OnDestroy()
        {
            MelonLogger.Msg("InteractableTrackerUpdater: Destroyed");
        }
    }
}
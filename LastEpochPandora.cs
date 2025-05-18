using LastEpochPandora.Mods;
using LastEpochPandora.Services;
using MelonLoader;
using MelonLoader.Properties;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastEpochPandora
{
    public class LastEpochPandora : MelonMod
    {

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Initializing mod...");
            SceneService.Init();
            

            SceneService.RegisterSceneChangeCallback(sceneName =>
            {
                if (!SceneService.InBannedScene() && ReferenceService.Instance == null)
                {
                    CreateServices();
                }
            });
        }

        private void CreateServices()
        {
            try
            {
                GameObject servicesHolder = new GameObject("Pandora_Services");
                GameObject.DontDestroyOnLoad(servicesHolder);
                servicesHolder.hideFlags = HideFlags.HideAndDontSave;

                var referenceService = servicesHolder.AddComponent<ReferenceService>();
                var espService = servicesHolder.AddComponent<ESPService>();
                referenceService.enabled = true;
                InteractableTracker.Initialize();
                ESPService.Initialize();

                MelonLogger.Msg($"Services created in scene: {SceneService._lastLoadedSceneName}");
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Failed to create services: {e}");
            }
        }

        public override void OnDeinitializeMelon()
        {
            MelonLogger.Msg("LastEpochPandora: OnDeinitializeMelon called.");
        }
    }
}
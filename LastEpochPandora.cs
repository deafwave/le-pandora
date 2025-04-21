using MelonLoader;
using UnityEngine;
using LastEpochPandora.Services;

namespace LastEpochPandora
{
    public class LastEpochPandora : MelonMod
    {
        private DamageService _damageServiceInstance;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("LastEpochPandora: OnInitializeMelon called.");

            SceneService.Init();
            MelonLogger.Msg("LastEpochPandora: SceneService initialized.");

            GameObject servicesHolder = new GameObject("Pandora_Services");

            ReferenceService referenceService = servicesHolder.AddComponent<ReferenceService>();
            referenceService.enabled = true;
            MelonLogger.Msg($"LastEpochPandora: ReferenceService added..., enabled: {referenceService.enabled}");

            _damageServiceInstance = servicesHolder.AddComponent<DamageService>();
            _damageServiceInstance.enabled = true;
            MelonLogger.Msg($"LastEpochPandora: DamageService added..., enabled: {_damageServiceInstance.enabled}");

            GameObject.DontDestroyOnLoad(servicesHolder);
        }

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("LastEpochPandora: OnLateInitializeMelon called.");
            MelonLogger.Msg($"Name: {this.Info.Name} | Version: {this.Info.Version}");
        }

        public override void OnDeinitializeMelon()
        {
            MelonLogger.Msg("LastEpochPandora: OnDeinitializeMelon called.");
            SceneService.DeInit();
        }
    }
}
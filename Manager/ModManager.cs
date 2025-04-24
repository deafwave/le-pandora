using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using LastEpochPandora.Services;
using LastEpochPandora.Mods;
using UnityEngine;

namespace LastEpochPandora
{
    public class ModManager
    {
        private static ModManager _instance;
        public static ModManager Instance => _instance ??= new ModManager();

        private readonly List<IMod> _mods = new List<IMod>();
        private GameObject _servicesHolder;

        private ModManager() { }

        public void Initialize()
        {
            MelonLogger.Msg("ModManager: Initializing...");
            _servicesHolder = new GameObject("Pandora_Services");
            GameObject.DontDestroyOnLoad(_servicesHolder);

            InitializeServices();

            RegisterMods();
            InitializeMods();

            MelonLogger.Msg("ModManager: Initialization complete.");
        }

        public void Deinitialize()
        {
            MelonLogger.Msg("ModManager: Deinitializing mods...");
            foreach (var mod in _mods)
            {
                try
                {
                    mod.Deinitialize();
                    MelonLogger.Msg($"ModManager: Deinitialized mod {mod.Name}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"ModManager: Failed to deinitialize mod {mod.Name}: {ex}");
                }
            }

            SceneService.DeInit();
        }

        public T GetMod<T>() where T : IMod
        {
            return _mods.OfType<T>().FirstOrDefault();
        }

        public IMod GetMod(string name)
        {
            return _mods.FirstOrDefault(m => m.Name == name);
        }

        private void InitializeServices()
        {
            MelonLogger.Msg("ModManager: Initializing core services...");
            SceneService.Init();
            MelonLogger.Msg("ModManager: SceneService initialized.");

            ReferenceService referenceService = _servicesHolder.AddComponent<ReferenceService>();
            referenceService.enabled = true;
            MelonLogger.Msg($"ModManager: ReferenceService added, enabled: {referenceService.enabled}");
        }

        private void RegisterMods()
        {
            MelonLogger.Msg("ModManager: Registering mods...");
            _mods.Add(new KillCountMod());
            _mods.Add(new MinimapMod());
           //_mods.Add(new FarInteractionsMod());
            _mods.Add(new ZoomMod());
            MelonLogger.Msg($"ModManager: Registered {_mods.Count} mods.");
        }

        private void InitializeMods()
        {
            MelonLogger.Msg("ModManager: Initializing mods...");
            foreach (var mod in _mods)
            {
                try
                {
                    mod.Initialize();
                    MelonLogger.Msg($"ModManager: Initialized mod {mod.Name} {mod.Version}");
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"ModManager: Failed to initialize mod {mod.Name}: {ex}");
                }
            }
        }
    }
}
using Il2Cpp;
using MelonLoader;
using HarmonyLib;
using System;
using LastEpochPandora.Services;
using UnityEngine;

namespace LastEpochPandora.Mods
{
    internal class ZoomMod : IMod
    {
        public string Name => "ZoomMod";
        public string Version => "1.0.0";

        private static float zoomAdd = 13f;
        private static CameraManager _cameraManager;

        public void Initialize()
        {
            MelonLogger.Msg($"ZoomMod: Initializing {Name} {Version}");

            ReferenceService.RegisterLocalPlayerAvailableCallback(() =>
            {
                MelonLogger.Msg("ZoomMod: LocalPlayer available callback received");
                ApplyZoom();
            });

            SceneService.RegisterSceneChangeCallback((_) =>
            {
                MelonLogger.Msg("ZoomMod: Scene changed, reapplying zoom");
                ApplyZoom();
            });
        }

        public void Deinitialize()
        {
            MelonLogger.Msg($"ZoomMod: Deinitializing {Name}");

            if (_cameraManager != null)
            {
                _cameraManager.zoomDefault = -17.5f;
                _cameraManager.ApplyZoom();
            }
        }

        private void ApplyZoom()
        {
            if (!ReferenceService.CanRun())
            {
                MelonLogger.Msg($"{Name}: Conditions not met for applying zoom");
                return;
            }

            try
            {
                if (_cameraManager == null && CameraManager.instance != null)
                {
                    _cameraManager = CameraManager.instance;
                }

                if (_cameraManager != null)
                {
                    float newZoomValue = -17.5f - zoomAdd;
                    if (_cameraManager.zoomDefault != newZoomValue)
                    {
                        MelonLogger.Msg($"{Name}: Setting zoom to {newZoomValue}");
                        _cameraManager.zoomDefault = newZoomValue;
                        _cameraManager.ApplyZoom();
                    }
                }
                else
                {
                    MelonLogger.Warning($"{Name}: CameraManager instance not found");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"{Name}: Failed to apply zoom: {ex}");
            }
        }
    }
}
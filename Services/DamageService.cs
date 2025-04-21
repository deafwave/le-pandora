using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using Il2Cpp;
using System.Collections;

namespace LastEpochPandora.Services
{
    [RegisterTypeInIl2Cpp]
    public class DamageService : MonoBehaviour
    {
        public DamageService(IntPtr ptr) : base(ptr) { }

        public static DamageService Instance { get; private set; }
        private HarmonyLib.Harmony _harmony;
        private const string HarmonyId = "com.deafwave.lastepochpandora.damageservice";
        private bool _isPatched = false;
        private static bool _waitingForActor = false;

        public static class DamageContext
        {
            public static bool Hit = false;
            public static bool Critical = false;
            public static bool Kill = false;
            public static Actor current_source_actor = null;
            public static Actor current_target_actor = null;
            public static Ability current_source_ability = null;
            public static float previous_health = -1;
        }

        public void Awake()
        {
            Instance = this;
            MelonLogger.Msg("DamageService: Awake() called and Instance set.");

            SceneService.RegisterSceneChangeCallback(OnSafeSceneLoaded);
            ReferenceService.RegisterLocalPlayerAvailableCallback(OnLocalPlayerReady);
        }

        [HideFromIl2Cpp]
        private void OnLocalPlayerReady()
        {
            if (!_isPatched && CanRun())
            {
                MelonLogger.Msg("DamageService: LocalPlayer is ready. Verifying actor before patching...");
                TryApplyPatchesWithRetry();
            }
        }

        [HideFromIl2Cpp]
        private void OnSafeSceneLoaded(string sceneName)
        {
            if (!_isPatched && CanRun())
            {
                MelonLogger.Msg($"DamageService: Scene '{sceneName}' is safe. Verifying actor before patching...");
                TryApplyPatchesWithRetry();
            }
        }

        private void TryApplyPatchesWithRetry()
        {
            var actor = ReferenceService.GetPlayerAsActor();
            if (actor == null)
            {
                if (!_waitingForActor)
                {
                    MelonLogger.Msg("DamageService: Player Actor not yet available. Starting coroutine to wait...");
                    _waitingForActor = true;
                    MelonCoroutines.Start(WaitForValidActor());
                }
                return;
            }

            ApplyPatches();
        }

        [HideFromIl2Cpp]
        private static IEnumerator WaitForValidActor()
        {
            while (ReferenceService.GetPlayerAsActor() == null)
            {
                MelonLogger.Msg("[ReferenceService] GameplayActor was null or destroyed.");
                yield return new WaitForSeconds(1f);
            }

            MelonLogger.Msg("DamageService: Player Actor resolved. Proceeding with patching.");
            _waitingForActor = false;
            Instance.ApplyPatches();
        }

        private void ApplyPatches()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony(HarmonyId);
                var originalMethod = typeof(DamageStatsHolder).GetMethod("applyDamage", new Type[] { typeof(Actor) });
                if (originalMethod == null)
                {
                    MelonLogger.Warning("DamageService: Original method applyDamage not found.");
                    return;
                }

                var prefix = typeof(DamageStatsHolder_ApplyDamagePatch).GetMethod("Prefix");
                var postfix = typeof(DamageStatsHolder_ApplyDamagePatch).GetMethod("Postfix");
                if (prefix == null || postfix == null)
                {
                    MelonLogger.Warning("DamageService: Prefix or Postfix method not found.");
                    return;
                }

                _harmony.Patch(originalMethod,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix));

                _isPatched = true;
                MelonLogger.Msg("DamageService: Harmony patches applied.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"DamageService: Failed to apply Harmony patches: {ex}");
            }
        }

        public static bool CanRun()
        {
            bool localPlayerValid = ReferenceService.LocalPlayer != null && !ReferenceService.LocalPlayer.IsNullOrDestroyed();
            bool notInBannedScene = !SceneService.InBannedScene();
            MelonLogger.Msg($"CanRun: LocalPlayer valid = {localPlayerValid}, In banned scene = {!notInBannedScene}");
            return localPlayerValid && notInBannedScene;
        }

        public static void Player_OnHealth(float health, string AbilityName)
        {
            string s = "Player Health for " + health + " from " + AbilityName;
            MelonLogger.Msg(s);
        }

        public static void Enemy_OnHealth(Actor enemy, float health, string AbilityName)
        {
            string s = enemy.name + " health for = " + health + " with " + AbilityName;
            MelonLogger.Msg(s);
        }

        public static float Get_Health(ref Actor actor)
        {
            float result = -1;
            if (actor == ReferenceService.LocalPlayer)
            {
                if (!actor.gameObject.GetComponent<PlayerHealth>().IsNullOrDestroyed())
                {
                    result = actor.gameObject.GetComponent<PlayerHealth>().currentHealth;
                }
                else { MelonLogger.Error("DamageStatsHolder:applyDamage Prefix : Can't get player health from target : " + actor.name); }
            }
            else
            {
                if (!actor.gameObject.GetComponent<UnitHealth>().IsNullOrDestroyed())
                {
                    result = actor.gameObject.GetComponent<UnitHealth>().currentHealth;
                }
                else { MelonLogger.Error("DamageStatsHolder:applyDamage Prefix : Can't get (enenmy or summoned) health from target : " + actor.name); }
            }
            return result;
        }

        private static bool Get_IsSummoned(Actor actor)
        {
            bool result = false;
            if (!ReferenceService.LocalPlayer.IsNullOrDestroyed())
            {
                SummonTracker summon_tracker = ReferenceService.LocalPlayer.gameObject.GetComponent<SummonTracker>();
                if (summon_tracker.IsNullOrDestroyed())
                {
                    foreach (Summoned summon in summon_tracker.summons)
                    {
                        if (summon.actor == actor)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(DamageStatsHolder), "applyDamage", new Type[] { typeof(Actor) })]
    public class DamageStatsHolder_ApplyDamagePatch
    {
        [HarmonyPrefix]
        public static void Prefix(DamageStatsHolder __instance, Actor __0)
        {
            MelonLogger.Msg("Entered Prefix (DamageStatsHolder.applyDamage)");
            if (DamageService.CanRun())
            {
                DamageService.DamageContext.Hit = false;
                DamageService.DamageContext.Critical = false;
                DamageService.DamageContext.Kill = false;
                DamageService.DamageContext.previous_health = DamageService.Get_Health(ref __0);
                DamageService.DamageContext.current_source_ability = __instance.GetDamageSourceInfo().Item2;
                DamageService.DamageContext.current_source_actor = __instance.GetDamageSourceInfo().Item3;
                DamageService.DamageContext.current_target_actor = __0;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(DamageStatsHolder __instance, Actor __0)
        {
            MelonLogger.Msg("Postfix entered (DamageStatsHolder.applyDamage)");
            if (DamageService.CanRun() && (!ReferenceService.LocalPlayer.IsNullOrDestroyed()) && (!SceneService.InBannedScene()))
            {
                float current_health = DamageService.Get_Health(ref __0);
                float damage_dealt = DamageService.DamageContext.previous_health - current_health;

                MelonLogger.Msg($"Damage Dealt: {damage_dealt}, Hit: {DamageService.DamageContext.Hit}, Critical: {DamageService.DamageContext.Critical}, Kill: {DamageService.DamageContext.Kill}");
                if (DamageService.DamageContext.current_source_ability != null)
                    MelonLogger.Msg($"Source Ability: {DamageService.DamageContext.current_source_ability.abilityName}");
                if (DamageService.DamageContext.current_source_actor != null)
                    MelonLogger.Msg($"Source Actor: {DamageService.DamageContext.current_source_actor.name}");
                if (DamageService.DamageContext.current_target_actor != null)
                    MelonLogger.Msg($"Target Actor: {DamageService.DamageContext.current_target_actor.name}");
            }
        }
    }
}
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

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
            MelonLogger.Msg($"DamageService.Awake(): ReferenceService.LocalPlayer is {(ReferenceService.LocalPlayer == null ? "null" : "not null")}");

            SceneService.RegisterSceneChangeCallback(OnSafeSceneLoaded);
            ReferenceService.RegisterLocalPlayerAvailableCallback(OnLocalPlayerReady);
        }

        [HideFromIl2Cpp]
        private void OnLocalPlayerReady()
        {
            if (!_isPatched && CanRun())
            {
                MelonLogger.Msg("DamageService: LocalPlayer is ready and CanRun passed. Applying patches...");
                ApplyPatches();
            }
            else
            {
                MelonLogger.Msg("DamageService: OnLocalPlayerReady called, but CanRun failed or already patched.");
            }
        }

        [HideFromIl2Cpp]
        private void OnSafeSceneLoaded(string sceneName)
        {
            if (!_isPatched && CanRun())
            {
                MelonLogger.Msg($"DamageService: Safe scene '{sceneName}' loaded and CanRun passed. Applying patches...");
                ApplyPatches();
            }
            else
            {
                MelonLogger.Msg($"DamageService: Scene '{sceneName}' loaded, but CanRun failed or already patched.");
            }
        }

        private void ApplyPatches()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony(HarmonyId);
                var originalMethod = typeof(DamageStatsHolder).GetMethod("applyDamage",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(Actor) },
                    null);

                MelonLogger.Msg($"Original method found: {originalMethod != null}");
                if (originalMethod == null)
                {
                    MelonLogger.Error("Could not find method DamageStatsHolder.applyDamage(Actor)");
                    var methods = typeof(DamageStatsHolder).GetMethods().Where(m => m.Name == "applyDamage").ToList();
                    MelonLogger.Msg($"Found {methods.Count} methods named 'applyDamage':");
                    foreach (var method in methods)
                    {
                        var parameters = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name));
                        MelonLogger.Msg($"  - {method.ReturnType.Name} {method.Name}({parameters})");
                    }
                    return;
                }

                var prefix = typeof(DamageStatsHolder_ApplyDamagePatch).GetMethod("Prefix");
                MelonLogger.Msg($"Prefix method found: {prefix != null}");
                if (prefix == null) return;

                var postfix = typeof(DamageStatsHolder_ApplyDamagePatch).GetMethod("Postfix");
                MelonLogger.Msg($"Postfix method found: {postfix != null}");
                if (postfix == null) return;

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

    public class DamageStatsHolder_ApplyDamagePatch
    {
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
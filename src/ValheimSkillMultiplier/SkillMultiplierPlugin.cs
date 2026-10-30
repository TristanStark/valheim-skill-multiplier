using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace ValheimSkillMultiplier
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class SkillMultiplierPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.tristanstark.valheim.skillmultiplier";
        public const string PluginName = "Valheim Skill Multiplier";
        public const string PluginVersion = "1.0.0";

        internal static ConfigEntry<float> XpMultiplier;

        private Harmony _harmony;

        private void Awake()
        {
            XpMultiplier = Config.Bind(
                "General",
                "Multiplier",
                2f,
                new ConfigDescription(
                    "Multiplier applied to XP gained by every skill. 1 = vanilla; 2 = double XP; 3 = triple XP; 0 = no XP.",
                    new AcceptableValueRange<float>(0f, 100f)));

            MethodInfo target = SkillGainPatch.FindTargetMethod();

            if (target == null)
            {
                Logger.LogError(
                    "Could not find Skills.RaiseSkill(SkillType, float). " +
                    "The game version may have changed; XP gain was left untouched.");
                return;
            }

            try
            {
                _harmony = new Harmony(PluginGuid);
                _harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(SkillGainPatch), nameof(SkillGainPatch.Prefix)));

                Logger.LogInfo(
                    "Patched Skills.RaiseSkill. Current multiplier: " +
                    XpMultiplier.Value + "x.");
            }
            catch (Exception error)
            {
                Logger.LogError("Failed to patch Skills.RaiseSkill: " + error);
                _harmony?.UnpatchSelf();
                _harmony = null;
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    /// <summary>
    /// Reflection is intentional: no proprietary Valheim assemblies need to be
    /// bundled with the plugin or installed on a CI build machine.
    /// </summary>
    internal static class SkillGainPatch
    {
        internal static MethodInfo FindTargetMethod()
        {
            Type skillsType = AccessTools.TypeByName("Skills");
            if (skillsType == null)
            {
                return null;
            }

            MethodInfo[] methods = skillsType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                if (method.Name != "RaiseSkill" || method.ReturnType != typeof(void))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 2 ||
                    !parameters[0].ParameterType.IsEnum ||
                    parameters[1].ParameterType != typeof(float))
                {
                    continue;
                }

                return method;
            }

            return null;
        }

        // Harmony binds 'factor' to the original method argument of the same name.
        internal static void Prefix(ref float factor)
        {
            // Only modify actual gains, not skill reductions or neutral calls.
            if (factor <= 0f || SkillMultiplierPlugin.XpMultiplier == null)
            {
                return;
            }

            float multiplier = SkillMultiplierPlugin.XpMultiplier.Value;

            // Config.Bind validates [0, 100], but still guard against invalid values.
            if (float.IsNaN(multiplier) ||
                float.IsInfinity(multiplier) ||
                multiplier < 0f ||
                multiplier > 100f)
            {
                return;
            }

            factor *= multiplier;
        }
    }
}

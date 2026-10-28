using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SAIN.Models.Preset.Personalities;
using SAIN.Preset.Shared.Models.Preset.Personalities;
using SAIN.Preset.Shared.Personalities.BasePersonality;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;

namespace SAINPersonalityMixer;

[BepInPlugin(Guid, "SAIN Personality Mixer", Version)]
[BepInDependency("me.sol.sain", "4.5.1")]
[BepInProcess("EscapeFromTarkov.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "com.chazut.sainpersonalitymixer";
    public const string Version = "1.0.0";
    private static PersonalityPatch? _patch;
    private Harmony? _harmony;
    private ConfigEntry<bool> _enabled = null!;
    private ConfigEntry<bool> _logAssignments = null!;
    private ConfigEntry<float>[] _chances = null!;
    private volatile Mix _mix = new(true, Mix.DefaultWeights());

    private void Awake()
    {
        _enabled = Config.Bind("1. General", "Enabled", true, new ConfigDescription(
            "Use this mix for newly spawned PMCs only. Existing bots keep their assigned personality until the raid ends. Scavs, bosses, followers and other bot roles keep SAIN's assignment.",
            null, new ConfigurationManagerAttributes { Order = 2 }));
        _logAssignments = Config.Bind("1. General", "Log assignments", false, new ConfigDescription(
            "Write one log line for each PMC assigned by this mod. Useful for checking the mix across multiple raids.",
            null, new ConfigurationManagerAttributes { Order = 1 }));
        var defaults = Mix.DefaultWeights();
        _chances = Enumerable.Range(0, Mix.Count).Select(i => Config.Bind("2. Personality chances",
            Mix.DisplayName((Personality)i), (float)defaults[i], new ConfigDescription(
                "Chance for NEW PMCs. Values totaling 100 are percentages; other totals are normalized automatically. Zero excludes this personality. SAIN's level, gear, nickname and forced-assignment rules are bypassed while this mod assigns the bot. Combat settings still come from your SAIN preset.",
                new AcceptableValueRange<float>(0, 100), new ConfigurationManagerAttributes { Order = 20 - i }))).ToArray();
        Config.Bind("2. Personality chances", "Effective chances", "", new ConfigDescription(
            "The normalized mix. Changes apply to new PMCs, not bots already in the raid. If all chances are zero, SAIN handles new assignments.",
            null, new ConfigurationManagerAttributes
            {
                Order = 0, HideDefaultButton = true,
                CustomDrawer = _ => GUILayout.Label(_mix.Summary, new GUIStyle(GUI.skin.label) { wordWrap = true })
            }));
        RebuildMix();
        _enabled.SettingChanged += (_, _) => RebuildMix();
        foreach (var entry in _chances) entry.SettingChanged += (_, _) => RebuildMix();

        try
        {
            var target = typeof(PersonalityDictionary).GetMethod("GetPersonality", BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(SAINBotInfoClass), typeof(PersonalitySettingsClass).MakeByRefType() }, null);
            if (target == null || target.ReturnType != typeof(EPersonality))
                throw new MissingMethodException("Compatible SAIN GetPersonality method was not found.");
            _patch = new PersonalityPatch(() => _mix, () => UnityEngine.Random.value,
                message => Logger.LogWarning(message), message =>
                {
                    if (_logAssignments.Value) Logger.LogInfo($"PMC personality: {message}");
                });
            _harmony = new Harmony(Guid);
            _harmony.Patch(target, prefix: new HarmonyMethod(typeof(Plugin), nameof(Prefix)) { priority = Priority.First });
            Logger.LogInfo($"SAIN Personality Mixer {Version} ready (PMCs only). {_mix.Summary}");
        }
        catch (Exception error)
        {
            _harmony?.UnpatchSelf();
            _patch = null;
            Logger.LogError($"Mixer could not start. SAIN assignment remains unchanged. {error}");
        }
    }

    private void RebuildMix() => _mix = new Mix(_enabled.Value, _chances.Select(c => (double)c.Value).ToArray());

    // Positional argument names also tolerate SAIN releases that rename parameters.
    private static bool Prefix(PersonalityDictionary __instance, SAINBotInfoClass __0,
        ref PersonalitySettingsClass __1, ref EPersonality __result)
        => _patch == null || _patch.Apply(__instance, __0, ref __1, ref __result);

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _patch = null;
    }
}

// BepInEx ConfigurationManager discovers these optional tags by their field names.
internal sealed class ConfigurationManagerAttributes
{
    public int? Order;
    public bool? HideDefaultButton;
    public Action<ConfigEntryBase>? CustomDrawer;
}

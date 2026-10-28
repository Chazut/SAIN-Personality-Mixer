using System;
using EFT;
using SAIN.Models.Preset.Personalities;
using SAIN.Preset.Shared.Models.Preset.Personalities;
using SAIN.Preset.Shared.Personalities.BasePersonality;
using SAIN.SAINComponent.Classes.Info;

namespace SAINPersonalityMixer;

internal sealed class PersonalityPatch
{
    private readonly AssignmentCache _cache = new();
    private readonly Func<Mix> _mix;
    private readonly Func<double> _random;
    private readonly Action<string> _warning;
    private readonly Action<string> _assignmentLog;
    private bool _exceptionReported;
    private bool _missingPresetReported;

    internal PersonalityPatch(Func<Mix> mix, Func<double> random, Action<string> warning, Action<string> assignmentLog)
    {
        _mix = mix;
        _random = random;
        _warning = warning;
        _assignmentLog = assignmentLog;
    }

    internal static bool IsPmc(WildSpawnType role) => role == WildSpawnType.pmcUSEC || role == WildSpawnType.pmcBEAR;

    internal bool Apply(PersonalityDictionary dictionary, SAINBotInfoClass info,
        ref PersonalitySettingsClass settings, ref EPersonality result)
    {
        // Check the bot being initialized, never its enemy, name or equipment.
        if (info?.Profile == null || !IsPmc(info.Profile.WildSpawnType)) return true;
        try
        {
            var assignment = _cache.Resolve(info, () => Create(dictionary, info));
            if (!assignment.Personality.HasValue) return true;
            var chosen = ToSain(assignment.Personality.Value);
            if (!dictionary.TryGetValue(chosen, out var currentSettings) || currentSettings == null)
            {
                if (!assignment.MissingReported)
                {
                    assignment.MissingReported = true;
                    _warning($"Cached personality {chosen} is missing from the current SAIN preset. Using SAIN's assignment for this bot.");
                }
                return true;
            }
            // Always return the settings from the CURRENT preset, not a stale cached object.
            settings = currentSettings;
            result = chosen;
            return false;
        }
        catch (Exception error)
        {
            if (!_exceptionReported)
            {
                _exceptionReported = true;
                _warning($"Personality assignment failed; falling back to SAIN. {error}");
            }
            return true;
        }
    }

    private Assignment Create(PersonalityDictionary dictionary, SAINBotInfoClass info)
    {
        var mix = _mix();
        bool missing = false;
        var personality = mix.Draw(p =>
        {
            bool present = dictionary.TryGetValue(ToSain(p), out var value) && value != null;
            missing |= !present;
            return present;
        }, _random);
        if (missing && !_missingPresetReported)
        {
            _missingPresetReported = true;
            _warning("Some weighted personalities are absent from the SAIN preset. Available personalities were reweighted; if none are available, SAIN handles assignment.");
        }
        if (personality.HasValue)
            _assignmentLog($"{info.Profile.WildSpawnType}: {info.BotOwner?.ProfileId ?? "unknown"} -> {Mix.DisplayName(personality.Value)}");
        return new Assignment(personality);
    }

    internal static EPersonality ToSain(Personality value) => value switch
    {
        Personality.Normal => EPersonality.Normal,
        Personality.Chad => EPersonality.Chad,
        Personality.Wreckless => EPersonality.Wreckless,
        Personality.Rat => EPersonality.Rat,
        Personality.SnappingTurtle => EPersonality.SnappingTurtle,
        Personality.Coward => EPersonality.Coward,
        Personality.Timmy => EPersonality.Timmy,
        Personality.GigaChad => EPersonality.GigaChad,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
}

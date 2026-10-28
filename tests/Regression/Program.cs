using System.Runtime.CompilerServices;
using System.Text.Json;
using EFT;
using SAIN.Models.Preset.Personalities;
using SAIN.Preset.Shared.Models.Preset.Personalities;
using SAIN.Preset.Shared.Personalities.BasePersonality;
using SAIN.SAINComponent.Classes.Info;
using SAINPersonalityMixer;

internal static class Program
{
    private static int _checks;
    private static readonly List<string> Groups = new();

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new Exception(message);
    }

    internal static void Main(string[] args)
    {
        Distribution();
        ScopeAndReload();
        Fallbacks();
        Lifetime();
        if (args.Length >= 3) Metadata.Check(args[0], args[1], args[2], Check);
        Console.WriteLine(JsonSerializer.Serialize(new { passed = _checks, groups = Groups }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static PersonalityDictionary Dictionary()
    {
        var result = new PersonalityDictionary();
        foreach (var personality in Enum.GetValues<Personality>())
        {
            var sain = PersonalityPatch.ToSain(personality);
            result.Add(sain, new PersonalitySettingsClass(sain));
        }
        return result;
    }

    private static SAINBotInfoClass Bot(int role = 52, int level = 1, float gear = 0) => new()
    {
        Profile = new() { WildSpawnType = (WildSpawnType)role, PlayerLevel = level, PowerLevel = gear }
    };

    private static PersonalityPatch Adapter(Func<Mix> mix, Func<double> roll, List<string> warnings, List<string>? logs = null)
        => new(mix, roll, warnings.Add, (logs ?? new List<string>()).Add);

    private static (bool Original, EPersonality Result, PersonalitySettingsClass Settings) Apply(
        PersonalityPatch patch, PersonalityDictionary dictionary, SAINBotInfoClass bot)
    {
        var settings = dictionary.TryGetValue(EPersonality.Normal, out var normal) ? normal : null!;
        var result = EPersonality.Normal;
        var original = patch.Apply(dictionary, bot, ref settings, ref result);
        return (original, result, settings);
    }

    private static void Distribution()
    {
        var mix = new Mix(true, Mix.DefaultWeights());
        double[] expected = { 30, 10, 10, 20, 10, 10, 5, 5 };
        double start = 0;
        foreach (var personality in Enum.GetValues<Personality>())
        {
            double weight = expected[(int)personality];
            Check(mix.Weight(personality) == weight, "Requested defaults changed");
            Check(mix.Draw(_ => true, () => (start + weight / 2) / 100) == personality, "Interval midpoint");
            Check(mix.Draw(_ => true, () => start / 100) == personality, "Interval lower boundary");
            Check(mix.Draw(_ => true, () => (start + weight - 0.000001) / 100) == personality, "Interval upper boundary");
            start += weight;
        }
        Check(mix.Draw(_ => true, () => 1) == Personality.GigaChad, "Inclusive Unity endpoint");
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -1, 1.01 })
            Check(mix.Draw(_ => true, () => bad) == null, "Invalid random input accepted");
        var normalized = new Mix(true, new double[] { 1, 1, 0, 0, 0, 0, 0, 0 });
        Check(normalized.Draw(_ => true, () => .49) == Personality.Normal, "Non-100 total first interval");
        Check(normalized.Draw(_ => true, () => .5) == Personality.Chad, "Non-100 total normalized incorrectly");
        Check(normalized.Draw(_ => true, () => 1) == Personality.Chad, "Zero chance selected at endpoint");
        Check(normalized.Summary.Contains("Normal: 50%") && normalized.Summary.Contains("Chad: 50%"), "Effective F12 values");
        Check(new Mix(true, new double[8]).Draw(_ => throw new Exception(), () => throw new Exception()) == null, "Zero mix must not roll or query preset");
        Check(new Mix(false, expected).Draw(_ => throw new Exception(), () => throw new Exception()) == null, "Disabled mix must not run");
        var invalid = new Mix(true, new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -3, 1000, 0, 0, 0 });
        Check(invalid.Draw(_ => true, () => .4) == Personality.SnappingTurtle, "Invalid config sanitization");
        Check(mix.Draw(p => p == Personality.Rat, () => .99) == Personality.Rat, "Missing presets not reweighted");
        Check(mix.Draw(_ => false, () => throw new Exception()) == null, "Empty preset fallback");
        var rng = new Random(87123);
        var counts = new int[8];
        for (int i = 0; i < 100000; i++) counts[(int)mix.Draw(_ => true, rng.NextDouble)!.Value]++;
        for (int i = 0; i < counts.Length; i++)
            Check(Math.Abs(counts[i] / 1000.0 - expected[i]) < .5, "Distribution biased: " + (Personality)i);
        Groups.Add("Weighted distribution: boundaries, normalization, zero/invalid values, seeded 100000 draws");
    }

    private static void ScopeAndReload()
    {
        var dictionary = Dictionary();
        var mix = new Mix(true, Mix.DefaultWeights());
        int rolls = 0;
        var warnings = new List<string>();
        var logs = new List<string>();
        var patch = Adapter(() => mix, () => { rolls++; return .35; }, warnings, logs);
        for (int role = -1; role <= 100; role++)
        {
            var result = Apply(patch, dictionary, Bot(role));
            bool pmc = role == 51 || role == 52;
            Check(result.Original != pmc, "Incorrect scope for role " + role);
            Check(result.Result == (pmc ? EPersonality.Chad : EPersonality.Normal), "Result mutated outside PMC scope");
            Check(ReferenceEquals(result.Settings, dictionary[pmc ? EPersonality.Chad : EPersonality.Normal]), "Settings mismatch");
        }
        Check(rolls == 2 && logs.Count == 2, "Non-PMC consumed randomness or logged assignment");
        Check(Apply(patch, dictionary, null!).Original, "Null bot guard");
        Check(Apply(patch, dictionary, new SAINBotInfoClass { Profile = null! }).Original, "Null profile guard");
        // SAIN's assignment gates must not override this mod's mix.
        dictionary[EPersonality.Chad].Assignment.Enabled = false;
        dictionary[EPersonality.Chad].Assignment.CanBeRandomlyAssigned = false;
        dictionary[EPersonality.Chad].Assignment.RandomlyAssignedChance = 0;
        dictionary[EPersonality.Chad].Assignment.MinLevel = 90;
        dictionary[EPersonality.Chad].Assignment.PowerLevelMin = 999;
        foreach (int level in new[] { 1, 15, 60, 100 })
            foreach (float gear in new[] { 0f, 50f, 300f, 1000f })
                Check(Apply(patch, dictionary, Bot(52, level, gear)).Result == EPersonality.Chad, "Level/gear/SAIN assignment leaked into mix");
        var bot = Bot();
        Check(Apply(patch, dictionary, bot).Result == EPersonality.Chad, "Initial cached assignment");
        int before = rolls;
        var nextDictionary = Dictionary();
        mix = new Mix(true, new double[] { 0, 0, 0, 100, 0, 0, 0, 0 });
        var reloaded = Apply(patch, nextDictionary, bot);
        Check(reloaded.Result == EPersonality.Chad && rolls == before, "Preset reload rerolled existing bot");
        Check(ReferenceEquals(reloaded.Settings, nextDictionary[EPersonality.Chad]), "Stale preset settings retained");
        Check(!ReferenceEquals(reloaded.Settings, dictionary[EPersonality.Chad]), "Old settings object retained");
        Check(Apply(patch, nextDictionary, Bot()).Result == EPersonality.Rat, "New bot ignored new F12 mix");
        mix = new Mix(false, Mix.DefaultWeights());
        Check(Apply(patch, nextDictionary, bot).Result == EPersonality.Chad, "Disabling changed an existing mixed bot");
        var disabledBot = Bot();
        Check(Apply(patch, nextDictionary, disabledBot).Original, "Disabled assigned new bot");
        mix = new Mix(true, Mix.DefaultWeights());
        Check(Apply(patch, nextDictionary, disabledBot).Original, "Enabling hijacked an existing unowned bot");
        Check(warnings.Count == 0, "Unexpected warning on normal assignments");
        Groups.Add("PMC roles only, no level/gear bias, stable personalities with fresh settings on reload");
    }

    private static void Fallbacks()
    {
        var dictionary = Dictionary();
        var warnings = new List<string>();
        var mix = new Mix(true, Mix.DefaultWeights());
        var patch = Adapter(() => mix, () => .35, warnings);
        var bot = Bot();
        Apply(patch, dictionary, bot);
        dictionary.Remove(EPersonality.Chad);
        Check(Apply(patch, dictionary, bot).Original, "Missing cached personality must fall back");
        Check(Apply(patch, dictionary, bot).Original && warnings.Count == 1, "Missing cached warning spam");
        dictionary[EPersonality.Chad] = new PersonalitySettingsClass(EPersonality.Chad);
        Check(Apply(patch, dictionary, bot).Result == EPersonality.Chad, "Cached choice lost after preset restoration");
        var onlyRat = new PersonalityDictionary { [EPersonality.Rat] = new PersonalitySettingsClass(EPersonality.Rat) };
        Check(Apply(patch, onlyRat, Bot()).Result == EPersonality.Rat, "Partial preset reweight");
        Check(Apply(patch, new PersonalityDictionary(), Bot()).Original, "No personalities fallback");
        mix = new Mix(true, new double[8]);
        var zeroBot = Bot();
        Check(Apply(patch, dictionary, zeroBot).Original, "Zero mix fallback");
        mix = new Mix(true, Mix.DefaultWeights());
        Check(Apply(patch, dictionary, zeroBot).Original, "Zero-mix fallback not stable for existing bot");
        var brokenWarnings = new List<string>();
        var broken = Adapter(() => mix, () => throw new Exception("simulated boundary failure"), brokenWarnings);
        Check(Apply(broken, dictionary, Bot()).Original, "Exception must preserve original SAIN method");
        Check(Apply(broken, dictionary, Bot()).Original && brokenWarnings.Count == 1, "Exception warning spam");
        Groups.Add("Missing presets, all-zero/disabled fallback, exception isolation");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference UseEphemeralBot(AssignmentCache cache)
    {
        var bot = new object();
        cache.Resolve(bot, () => new Assignment(Personality.Rat));
        return new WeakReference(bot);
    }

    private static void Lifetime()
    {
        var cache = new AssignmentCache();
        var key = new object();
        int created = 0;
        Parallel.For(0, 40, _ => cache.Resolve(key, () => { Interlocked.Increment(ref created); return new Assignment(Personality.Rat); }));
        Check(created == 1, "Concurrent initialization rerolled a bot");
        var reference = UseEphemeralBot(cache);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(!reference.IsAlive, "Cache retains dead bot keys");
        GC.KeepAlive(cache);
        Groups.Add("One cached draw, weak bot lifetime");
    }
}

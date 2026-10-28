using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SAINPersonalityMixer;

internal enum Personality
{
    Normal, Chad, Wreckless, Rat, SnappingTurtle, Coward, Timmy, GigaChad
}

// Independent of SAIN's sequential assignment checks and equipment thresholds.
internal sealed class Mix
{
    internal const int Count = 8;
    private readonly double[] _weights;
    internal bool Enabled { get; }
    internal string Summary { get; }

    internal static double[] DefaultWeights() => new double[] { 30, 10, 10, 20, 10, 10, 5, 5 };

    internal Mix(bool enabled, double[] weights)
    {
        if (weights.Length != Count) throw new ArgumentException("Eight personality weights are required.", nameof(weights));
        Enabled = enabled;
        _weights = weights.Select(w => double.IsNaN(w) || double.IsInfinity(w) ? 0 : Math.Max(0, Math.Min(100, w))).ToArray();
        double total = _weights.Sum();
        Summary = !enabled ? "Disabled for new PMCs. Existing mixed bots keep their personality until the raid ends."
            : total <= 0 ? "All chances are zero. New PMCs use SAIN's normal assignment."
            : string.Join(" | ", Enumerable.Range(0, Count).Select(i =>
                $"{DisplayName((Personality)i)}: {(_weights[i] * 100 / total).ToString("0.##", CultureInfo.InvariantCulture)}%"));
    }

    internal static string DisplayName(Personality personality) => personality switch
    {
        Personality.SnappingTurtle => "Snapping Turtle",
        Personality.GigaChad => "GigaChad",
        _ => personality.ToString()
    };

    internal double Weight(Personality personality) => _weights[(int)personality];

    internal Personality? Draw(Func<Personality, bool> available, Func<double> random)
    {
        if (!Enabled) return null;
        // Only read the preset when assigning a new PMC, never in an Update loop.
        var effective = new double[Count];
        double total = 0;
        for (int i = 0; i < Count; i++)
            if (_weights[i] > 0 && available((Personality)i)) total += effective[i] = _weights[i];
        if (total <= 0) return null;
        double roll = random();
        if (double.IsNaN(roll) || double.IsInfinity(roll) || roll < 0 || roll > 1) return null;
        double remaining = roll * total;
        Personality? last = null;
        for (int i = 0; i < Count; i++)
        {
            if (effective[i] <= 0) continue;
            last = (Personality)i;
            if (remaining < effective[i]) return last;
            remaining -= effective[i];
        }
        return last; // Unity's Random.value can equal 1.
    }
}

internal sealed class Assignment
{
    internal Personality? Personality { get; }
    internal bool MissingReported { get; set; }
    internal Assignment(Personality? personality) => Personality = personality;
}

internal sealed class AssignmentCache
{
    // SAIN calls GetPersonality during both construction and configuration, and again
    // when its preset changes. A weak key retains one draw without keeping dead bots alive.
    private readonly ConditionalWeakTable<object, Assignment> _assignments = new();
    private readonly object _gate = new();

    internal Assignment Resolve(object bot, Func<Assignment> create)
    {
        lock (_gate)
        {
            if (_assignments.TryGetValue(bot, out var assignment)) return assignment;
            assignment = create();
            _assignments.Add(bot, assignment);
            return assignment;
        }
    }
}

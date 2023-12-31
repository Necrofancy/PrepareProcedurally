using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Necrofancy.PrepareProcedurally.Test.SkillLockIn;

public static class StaticData
{
    internal const string Shooting = "Shooting";
    internal const string Melee = "Melee";
    internal const string Construction = "Construction";
    internal const string Mining = "Mining";
    internal const string Cooking = "Cooking";
    internal const string Plants = "Plants";
    internal const string Animals = "Animals";
    internal const string Crafting = "Crafting";
    internal const string Artistic = "Artistic";
    internal const string Medical = "Medical";
    internal const string Social = "Social";
    internal const string Intellectual = "Intellectual";

    internal static readonly string[] RimworldSkills =
    {
        Shooting,
        Melee,
        Construction,
        Mining,
        Cooking,
        Plants,
        Animals,
        Crafting,
        Artistic,
        Medical,
        Social,
        Intellectual
    };

    internal static readonly IntRange Baseline = new(0, 4);

    internal static Dictionary<string, IntRange> FromShorthand(Dictionary<string, IntRange> sparse)
    {
        var dict = new Dictionary<string, IntRange>();
        dict.AddRange(sparse);
        foreach (var skill in RimworldSkills.Where(s => !dict.TryGetValue(s, out _)))
            dict[skill] = Baseline;
        return dict;
    }

    internal static readonly Dictionary<string, IntRange> AllBaseline = new();

    internal static readonly Dictionary<string, IntRange> TribeChildTender = new()
    {
        { Shooting, new IntRange(2, 6) },
        { Melee, new IntRange(2, 6) },
        { Plants, new IntRange(2, 6) },
        { Crafting, new IntRange(2, 6) },
        { Medical, new IntRange(3, 8) },
        { Cooking, new IntRange(3, 8) },
        { Animals, new IntRange(0, 10) },
        { Intellectual, new IntRange(0, 1) }
    };
}
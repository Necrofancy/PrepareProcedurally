using System;
using System.Collections.Generic;
using System.Text;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Verse;

namespace Necrofancy.PrepareProcedurally.Test.SkillLockIn;

public class StringSkillBuilder : SkillRangeBuilder<string>
{
    private static readonly HashSet<string> Empty = new();

    private StringSkillBuilder(Dictionary<string, IntRange> skillRanges, HashSet<string> forcedPassions,
        HashSet<string> disallowedPassions, float maxPassionPoints) : base(skillRanges, forcedPassions,
        disallowedPassions, maxPassionPoints)
    {
    }

    public static StringSkillBuilder Make(Dictionary<string, IntRange> skillRanges, HashSet<string> forced = null,
        HashSet<string> disallowed = null, float maxPassionPoints = 7.0f)
    {
        var skills = StaticData.FromShorthand(skillRanges);
        return new StringSkillBuilder(skills, forced ?? Empty, disallowed ?? Empty, maxPassionPoints);
    }

    internal string GetFinalResult()
    {
        var builder = new StringBuilder();
        var (result, exhausted) = Build();

        foreach (var skill in GetAllSkillDefinitions())
        {
            var skillRange = result[skill];
            var min = skillRange.Min;
            var max = skillRange.Max;
            var passion = skillRange.Passion;
            builder.AppendFormat("{0} {1}~{2}{3} {4}", skill.PadRight(13), min, max, passion.AsFireEmojis(),
                Environment.NewLine);
        }

        return builder.ToString();
    }

    protected override IReadOnlyList<string> GetAllSkillDefinitions()
    {
        return StaticData.RimworldSkills;
    }
}
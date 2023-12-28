using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public readonly struct BackgroundPossibility
{
    public BackgroundPossibility(BioPossibility background, IReadOnlyDictionary<SkillDef, IntRange> skillRanges,
        float assumedAge, bool canChange)
    {
        Background = background;
        SkillRanges = skillRanges;
        AssumedAge = assumedAge;
        CanChange = canChange;
    }

    public BioPossibility Background { get; }
    public IReadOnlyDictionary<SkillDef, IntRange> SkillRanges { get; }
    public float AssumedAge { get; }
    public bool CanChange { get; }
}
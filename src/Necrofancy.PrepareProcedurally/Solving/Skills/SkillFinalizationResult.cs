using System.Collections.Generic;
using RimWorld;

namespace Necrofancy.PrepareProcedurally.Solving.Skills;

public readonly struct SkillFinalizationResult
{
    public SkillFinalizationResult(IReadOnlyDictionary<SkillDef, PassionAndLevel> finalRanges, bool validVanillaPawn)
    {
        FinalRanges = finalRanges;
        ValidVanillaPawn = validVanillaPawn;
    }

    public bool ValidVanillaPawn { get; }
    public IReadOnlyDictionary<SkillDef, PassionAndLevel> FinalRanges { get; }
}
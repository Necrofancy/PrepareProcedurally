using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Skills
{
    public readonly struct SkillFinalizationResult
    {
        public SkillFinalizationResult(IReadOnlyDictionary<SkillDef, PassionAndLevel> finalRanges, bool validVanillaPawn)
        {
            FinalRanges = finalRanges;
            ValidVanillaPawn = validVanillaPawn;
        }

        public bool ValidVanillaPawn { get; }
        public IReadOnlyDictionary<SkillDef, PassionAndLevel> FinalRanges { get; }

        public void ApplyTo(Pawn pawn)
        {
            foreach (var skillRecord in pawn.skills.skills)
            {
                var passionAndLevel = FinalRanges[skillRecord.def];
                skillRecord.levelInt = Rand.RangeInclusive(passionAndLevel.Min, passionAndLevel.Max);
                skillRecord.passion = passionAndLevel.Passion;
            }
        }
    }
}
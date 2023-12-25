using System.Collections.Generic;
using System.Linq;
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
            var passionsModdedByGenes = new Dictionary<SkillDef, PassionMod.PassionModType>();
            foreach (var gene in pawn.genes.GenesListForReading.Where(x => x.Active && x.def.passionMod != null))
            {
                passionsModdedByGenes[gene.def.passionMod.skill] = gene.def.passionMod.modType;
            }
            foreach (var skillRecord in pawn.skills.skills)
            {
                var passionAndLevel = FinalRanges[skillRecord.def];
                skillRecord.levelInt = Rand.RangeInclusive(passionAndLevel.Min, passionAndLevel.Max);
                if (passionsModdedByGenes.TryGetValue(skillRecord.def, out var mod))
                {
                    switch (mod)
                    {
                        case PassionMod.PassionModType.AddOneLevel:
                            skillRecord.passion = passionAndLevel.Passion == Passion.Major
                                ? Passion.Major
                                : passionAndLevel.Passion + 1;
                            break;
                        case PassionMod.PassionModType.DropAll:
                            skillRecord.passion = Passion.None;
                            break;
                        default:
                            skillRecord.passion = passionAndLevel.Passion;
                            break;
                    }
                }
                else
                {
                    skillRecord.passion = passionAndLevel.Passion;
                }
            }
        }
    }
}
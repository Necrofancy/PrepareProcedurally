using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds
{
    public readonly struct BackgroundPossibility
    {
        public BackgroundPossibility(BioPossibility background, IReadOnlyDictionary<SkillDef, IntRange> skillRanges, bool canChange)
        {
            Background = background;
            SkillRanges = skillRanges;
            CanChange = canChange;
        }

        public BioPossibility Background { get; }
        public IReadOnlyDictionary<SkillDef, IntRange> SkillRanges { get; }
        public bool CanChange { get; }
    }
}
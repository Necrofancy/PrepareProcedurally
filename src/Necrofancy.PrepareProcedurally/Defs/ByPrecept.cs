using System.Collections.Generic;
using RimWorld;
using Verse;
// ReSharper disable UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Defs
{
    public class ByPrecept : Def
    {
        public List<PreceptDef> relatedPrecepts;
        public List<SkillRequirementDef> skillRequirements;
    }
}
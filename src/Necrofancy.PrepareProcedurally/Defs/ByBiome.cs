using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Defs
{
    public class ByBiome : Def
    {
        public List<BiomeDef> biome;
        public List<SkillRequirementDef> skillRequirements;
        public FloatRange? tribeMelanin;
    }
}
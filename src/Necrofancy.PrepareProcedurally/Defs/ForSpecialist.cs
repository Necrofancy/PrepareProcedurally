using System.Collections.Generic;
using RimWorld;
using Verse;

// ReSharper disable All

namespace Necrofancy.PrepareProcedurally.Defs
{
    public class ForSpecialist : Def
    {
        public PreceptDef IdeologyRole;

        public Dictionary<SkillDef, int> WeightingForSkills;
    }
}
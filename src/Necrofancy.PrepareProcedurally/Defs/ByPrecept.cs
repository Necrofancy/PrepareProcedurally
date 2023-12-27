using System.Collections.Generic;
using RimWorld;
using Verse;

// ReSharper disable All

namespace Necrofancy.PrepareProcedurally.Defs;

public class ByPrecept : Def
{
    public List<PreceptDef> relatedPrecepts;
    public List<SkillRequirementDef> skillRequirements;
}
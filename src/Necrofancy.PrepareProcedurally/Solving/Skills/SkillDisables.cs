using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving.Skills;

public static class SkillDisables
{
    public static IEnumerable<SkillDef> GetSkillsDisabled(BackstoryDef child, BackstoryDef adult)
    {
        var disables = child.workDisables;
        IEnumerable<WorkTypeDef> workDisables = child.DisabledWorkTypes;
        if (adult != null)
        {
            disables |= adult.workDisables;
            workDisables = workDisables.Concat(adult.DisabledWorkTypes);
        }

        foreach (var skill in DefDatabase<SkillDef>.AllDefsListForReading)
            if (skill.IsDisabled(disables, workDisables))
                yield return skill;
    }
}
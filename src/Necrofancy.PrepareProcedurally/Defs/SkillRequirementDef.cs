using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Defs;

public class SkillRequirementDef : Def
{
    public SkillDef skill;
    public WorkTags requiredWork;
    public int level = 0;
    public Passion passion = Passion.None;
    public bool allPawns = false;
    public SimpleCurve populationCurve;
        

    public int Count(int colonySize) => allPawns ? colonySize : (int) populationCurve.Evaluate(colonySize);

    public bool Satisfied(IReadOnlyList<SkillFinalizationResult> finalizations)
    {
        var goalsLeft = Count(finalizations.Count);

        foreach (var item in finalizations)
        {
            var passionLevel = item.FinalRanges[skill];
            if (passionLevel.Min >= level && passionLevel.Passion >= passion)
                goalsLeft--;
        }

        return goalsLeft <= 0;
    }
}
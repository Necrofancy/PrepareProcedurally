using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Weighting;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces.Solving;

public class AlienBalancingSituation
{
    public AlienBalancingSituation(IReadOnlyList<AlienCategories> backstoryCategories, int pawns,
        IReadOnlyList<SkillPassionSelection> skillRequirements)
    {
        BackstoryCategories = backstoryCategories;
        Pawns = pawns;
        SkillRequirements = skillRequirements;
    }

    public IReadOnlyList<AlienCategories> BackstoryCategories { get; }
    public int Pawns { get; }
    public IReadOnlyList<SkillPassionSelection> SkillRequirements { get; }
}
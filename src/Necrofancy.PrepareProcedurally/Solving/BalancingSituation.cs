using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;

namespace Necrofancy.PrepareProcedurally.Solving;

public class BalancingSituation
{
    public BalancingSituation(string name, List<BackstoryCategoryFilter> categoryFilters, int pawns, IReadOnlyList<SkillPassionSelection> skillRequirements)
    {
        Name = name;
        CategoryFilters = categoryFilters;
        Pawns = pawns;
        SkillRequirements = skillRequirements;
    }
        
    public string Name { get; }
    public List<BackstoryCategoryFilter> CategoryFilters { get; }
    public int Pawns { get; }
        
    public IReadOnlyList<SkillPassionSelection> SkillRequirements { get; }
}
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Defs;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving;

public static class SituationFactory
{
    public static BalancingSituation FromPlayerData()
    {
        var pawnCount = Find.GameInitData.startingPawnCount;
        var backstoryCategories = Faction.OfPlayer.def.backstoryFilters;

        var requirements = new List<SkillRequirementDef>();
        var ideoligion = Faction.OfPlayer.ideos.PrimaryIdeo;
        var tile = Find.GameInitData.startingTile;
        var terrain = Find.World.grid[tile];

        requirements.AddRange(BySetupOf.Basic.GetRequirements(terrain.Biomes.FirstOrDefault(), terrain.hilliness).Where(Relevant));

        if (ideoligion is not null)
        {
            foreach (var preceptLink in DefDatabase<ByPrecept>.AllDefsListForReading)
            {
                if (preceptLink.relatedPrecepts?.Any(ideoligion.HasPrecept) == true)
                    requirements.AddRange(preceptLink.skillRequirements.Where(Relevant));
            }
        }
        
        var selections = SkillPassionSelection.FromReqs(requirements, pawnCount);

        return new BalancingSituation(string.Empty, backstoryCategories, pawnCount, selections);

        bool Relevant(SkillRequirementDef def) => def.Count(pawnCount) > 0;
    }
}
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Defs;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public static class SituationFactory
    {
        public static BalancingSituation FromPlayerData()
        {
            int pawnCount = Find.GameInitData.startingPawnCount;
            string backstoryCategory = Faction.OfPlayer.def.backstoryFilters.First().categories.First();

            var requirements = new List<SkillRequirementDef>();
            var ideo = Faction.OfPlayer.ideos.PrimaryIdeo;
            int tile = Find.GameInitData.startingTile;
            var terrain = Find.World.grid[tile];

            bool Relevant(SkillRequirementDef def) => def.Count(pawnCount) > 0;
            
            requirements.AddRange(BySetupOf.Basic.GetRequirements(terrain.biome, terrain.hilliness).Where(Relevant));
            
            foreach (var ideoDef in DefDatabase<ByPrecept>.AllDefsListForReading)
            {
                if (ideoDef.relatedPrecepts?.Any(ideo.HasPrecept) == true)
                    requirements.AddRange(ideoDef.skillRequirements.Where(Relevant));
            }

            var selections = SkillPassionSelection.FromReqs(requirements, pawnCount);

            return new BalancingSituation(string.Empty, backstoryCategory, pawnCount, selections);
        }
    }
}
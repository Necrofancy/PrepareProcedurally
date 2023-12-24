using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

// ReSharper disable All

namespace Necrofancy.PrepareProcedurally.Defs
{
    public class BySetup : Def
    {
        public Dictionary<BiomeDef, RequirementSetDef> byBiomeDef;
        public Dictionary<Hilliness, RequirementSetDef> byBiomeTerrain;
        public List<SkillRequirementDef> baseRequirements;

        public IEnumerable<SkillRequirementDef> GetRequirements(BiomeDef def, Hilliness hilliness)
        {
            // ReSharper disable once InlineOutVariableDeclaration - C# compiler gets angy if it is inlined.
            RequirementSetDef reqDef;
            if (byBiomeDef?.TryGetValue(def, out reqDef) == true && reqDef?.requirements != null)
                foreach (var req in reqDef.requirements)
                    yield return req;

            if (byBiomeTerrain?.TryGetValue(hilliness, out reqDef) == true && reqDef?.requirements != null)
                foreach (var req in reqDef.requirements)
                    yield return req;

            foreach (var req in baseRequirements)
                yield return req;
        }
    }
}
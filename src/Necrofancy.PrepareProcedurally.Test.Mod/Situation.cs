using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Defs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Necrofancy.PrepareProcedurally.Test.Mod
{
    public class Situation
    {
        public Situation(string fileName, string description, BalancingSituation toBalance)
        {
            FileName = fileName;
            Description = description;
            ToBalance = toBalance;
        }

        public string FileName { get; }
        public string Description { get; }
        public BalancingSituation ToBalance { get; }

        public static IEnumerable<Situation> GenerateAll()
        {
            foreach ((string category, int size) in StandardScenarios())
            foreach (var setup in DefDatabase<BySetup>.AllDefsListForReading)
            foreach (var terrain in StandardTerrains())
            foreach (var ideology in DefDatabase<IdeoSetDef>.AllDefsListForReading)
            {
                var reqs = setup.GetRequirements(BiomeDefOf.TemperateForest, terrain).ToList();
                foreach (var preceptReqs in DefDatabase<ByPrecept>.AllDefs)
                {
                    if (ideology.givenPrecepts.Any(preceptReqs.relatedPrecepts.Contains))
                        reqs.AddRange(preceptReqs.skillRequirements);
                }

                string fileName = $"{setup.defName}_{ideology.defName}_{category}_{size}_{terrain}";

                var selection = SkillPassionSelection.FromReqs(reqs, size);
                var situation = new BalancingSituation(setup.defName, category, size, selection);
                yield return new Situation(fileName, ideology.description, situation);
            }
        }

        private static IEnumerable<(string Category, int Size)> StandardScenarios()
        {
            yield return ("Offworld", 3);
            yield return ("Tribal", 5);
            yield return ("Offworld", 1);
        }

        private static IEnumerable<Hilliness> StandardTerrains()
        {
            yield return Hilliness.Flat;
            yield return Hilliness.SmallHills;
            yield return Hilliness.LargeHills;
            yield return Hilliness.Mountainous;
        }
    }
}
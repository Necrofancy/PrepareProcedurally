using System.Collections.Generic;
using System.Linq;
using AlienRace;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.HumanoidAlienRaces
{
    [StaticConstructorOnStartup]
    // ReSharper disable once UnusedType.Global
    public class HumanoidAlienRaceCompatibility : Compatibility
    {
        static HumanoidAlienRaceCompatibility()
        {
            Layer = new HumanoidAlienRaceCompatibility();
        }
        
        public override IEnumerable<PawnKindDef> GetPawnKindsThatCanAlsoGenerateFor(FactionDef def)
        {
            return from alienSettings in DefDatabase<RaceSettings>.AllDefsListForReading
                from factionPawnKind in
                    alienSettings.pawnKindSettings.startingColonists.Where(x => x.factionDefs.Contains(def))
                from pawnKind in factionPawnKind.pawnKindEntries.SelectMany(pawnKindEntry => pawnKindEntry.kindDefs)
                select pawnKind;
        }

        public override int GetMinimumAgeForAdulthood(PawnKindDef kind)
        {
            return kind.race is ThingDef_AlienRace race
                ? (int)race.alienRace.generalSettings.minAgeForAdulthood
                : 20;
        }

        public override int GetMaximumGeneratedTraits(Pawn pawn)
        {
            return pawn.def is ThingDef_AlienRace alienDef
                ? alienDef.alienRace.generalSettings.additionalTraits.max + 3
                : 3;
        }

        public override Pawn RandomizeSingularPawn(Pawn pawn, CollectSpecificPassions collector, List<(SkillDef Skill, UsabilityRequirement Usability)> reqs)
        {
            return AlienProcGen.RandomizeSingularPawn(pawn, collector, reqs);
        }

        public override void RandomizeForTeam(BalancingSituation situation)
        {
            AlienProcGen.RandomizeTeam(situation);
        }
    }
}
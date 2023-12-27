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

        public override IEnumerable<TraitRequirement> GetExtraTraitRequirements(Pawn pawn)
        {
            List<TraitRequirement> requirements = new List<TraitRequirement>();
            
            if (pawn.def is ThingDef_AlienRace defAlien && defAlien.alienRace.generalSettings.forcedRaceTraitEntries
                is {} traits)
            {
                foreach (var entry in traits)
                {
                    TraitDef actualDef = entry.defName;
                    
                    float commonality = pawn.gender switch
                    {
                        Gender.Male => entry.commonalityMale,
                        Gender.Female => entry.commonalityFemale,
                        _ => -1f
                    };
                    
                    if (entry.chance >= 100 && commonality < 0)
                    {
                        requirements.Add(new TraitRequirement{def = actualDef, degree = entry.degree});
                    }
                }
            }

            return requirements;
        }

        public override int GetMinimumAgeForAdulthood(PawnKindDef kind)
        {
            return kind.race is ThingDef_AlienRace race
                ? (int)race.alienRace.generalSettings.minAgeForAdulthood
                : 20;
        }

        /// <summary>
        /// From HAR mods I've tried, trying to change this either does nothing, or explodes on non-humans.
        /// </summary>
        public override bool AllowEditingBodyType(Pawn pawn)
        {
            return pawn.def.defName.Equals("human");
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
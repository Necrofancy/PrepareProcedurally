using System.Collections.Generic;
using System.Linq;
using AlienRace;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
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
            Log.Error("Prepare Procedurally - Loaded Compatibility for Human Alien Races");
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
            var index = StartingPawnUtility.PawnIndex(pawn);

            var addBackToLocked = false;
            if (ProcGen.LockedPawns.Contains(pawn))
            {
                ProcGen.LockedPawns.Remove(pawn);
                addBackToLocked = true;
            }

            string pawnChildhoods = null, pawnAdulthoods = null;
            using (TemporarilyChange.PlayerFactionMelaninRange(ProcGen.MelaninRange))
            using (TemporarilyChange.AgeOnAllRelevantRaceProperties(ProcGen.RaceAgeRanges))
            {
                bool foundBackstoryToWorkWith = false;
                while (!foundBackstoryToWorkWith)
                {
                    pawn = StartingPawnUtility.RandomizeInPlace(pawn);
                    ProcGen.OnPawnChanged(pawn);

                    if (pawn.story.Childhood.spawnCategories.Any() &&
                        pawn.story.Adulthood?.spawnCategories.Any() == true)
                    {
                        pawnChildhoods = pawn.story.Childhood.spawnCategories.First();
                        pawnAdulthoods = pawn.story.Adulthood.spawnCategories.First();
                        foundBackstoryToWorkWith = true;
                    }
                }
            }
            
            var specifier = new SelectBackstorySpecifically(new List<string>{pawnChildhoods, pawnAdulthoods});
            var bio = specifier.GetBestBio(collector.Weight, ProcGen.TraitRequirements[index]);
            var traits = bio.Traits;

            var builder = new PawnBuilder(bio);
            foreach (var (skill, usability) in reqs.OrderBy(x => x.Usability).ThenByDescending(x => x.Skill.listOrder))
            {
                if (usability == UsabilityRequirement.Major)
                    builder.TryLockInPassion(skill, Passion.Major);
                else if (usability == UsabilityRequirement.Minor)
                    builder.TryLockInPassion(skill, Passion.Minor);
            }
            
            bio.ApplyBackstoryTo(pawn);
            builder.Build().ApplySimulatedSkillsTo(pawn);
            traits.ApplyRequestedTraitsTo(pawn);

            if (addBackToLocked)
            {
                ProcGen.LockedPawns.Add(pawn);
            }

            return pawn;
        }

        public override void RandomizeForTeam(BalancingSituation situation)
        {
            // it's actually impossible to try balancing up-front because I don't know what backstories are available
            // so at each step let's try grabbing some passions and go through each unlocked pawn.

            var variation = new IntRange(10, (int)(ProcGen.SkillWeightVariation * 10));
            var backgrounds = BackstorySolver.TryToSolveWith(situation, variation);
            var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, situation);
            ProcGen.LastResults = finalSkills;
            
            for (int pawnIndex = 0; pawnIndex < ProcGen.StartingPawns.Count; pawnIndex++)
            {
                var pawn = ProcGen.StartingPawns[pawnIndex];
                if (ProcGen.LockedPawns.Contains(pawn) || !ProcGen.LastResults[pawnIndex].HasValue)
                {
                    continue;
                }
                
                var reqs = new List<(SkillDef Skill, UsabilityRequirement Usability)>();
                foreach (var skill in DefDatabase<SkillDef>.AllDefsListForReading)
                {
                    var result = ProcGen.LastResults[pawnIndex].Value.FinalRanges[skill];
                    var usability = result.Passion switch
                    {
                        Passion.Major => UsabilityRequirement.Major,
                        Passion.Minor => UsabilityRequirement.Minor,
                        _ => UsabilityRequirement.CanBeOff
                    };
                    reqs.Add((skill, usability));
                }
                
                var dict = new Dictionary<SkillDef, int>();
                var requiredSkills = new List<SkillDef>();
                var requiredWorkTags = WorkTags.None;

                foreach (var (skill, usability) in reqs)
                {
                    switch (usability)
                    {
                        case UsabilityRequirement.Major:
                            dict[skill] = 20 * variation.RandomInRange;
                            requiredWorkTags |= skill.disablingWorkTags;
                            requiredSkills.Add(skill);
                            break;
                        case UsabilityRequirement.Minor:
                            dict[skill] = 10 * variation.RandomInRange;
                            requiredWorkTags |= skill.disablingWorkTags;
                            requiredSkills.Add(skill);
                            break;
                        case UsabilityRequirement.Usable:
                            dict[skill] = 5 * variation.RandomInRange;
                            requiredWorkTags |= skill.disablingWorkTags;
                            requiredSkills.Add(skill);
                            break;
                        default:
                            dict[skill] = -5 * variation.RandomInRange;
                            break;
                    }
                }

                foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    if (requiredSkills.Any(requiredSkills.Contains))
                    {
                        requiredWorkTags |= workType.workTags;
                    }
                }
            
                var collectSpecificPassions = new CollectSpecificPassions(dict, requiredWorkTags);
                ProcGen.StartingPawns[pawnIndex] = RandomizeSingularPawn(pawn, collectSpecificPassions, reqs);
            }
        }
    }
}
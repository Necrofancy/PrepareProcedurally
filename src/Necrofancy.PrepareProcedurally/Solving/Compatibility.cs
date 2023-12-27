using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public class Compatibility
    {
        public static Compatibility Layer { get; protected set; } = new Compatibility();
        
        public virtual int GetMaximumGeneratedTraits(Pawn pawn) => 3;
        
        public virtual IEnumerable<TraitRequirement> GetExtraTraitRequirements(Pawn pawn) => Enumerable.Empty<TraitRequirement>();
        
        public virtual int GetMinimumAgeForAdulthood(PawnKindDef kind) => 20;
        
        public virtual IEnumerable<PawnKindDef> GetPawnKindsThatCanAlsoGenerateFor(FactionDef def) => Enumerable.Empty<PawnKindDef>();
        
        public virtual bool AllowEditingBodyType(Pawn pawn) => true;

        public virtual void RandomizeForTeam(BalancingSituation situation)
        {
            var pawnList = Find.GameInitData.startingAndOptionalPawns;
            var pawnCount = Find.GameInitData.startingPawnCount;

            var empty = new List<TraitDef>();

            var variation = new IntRange(10, (int)(ProcGen.SkillWeightVariation * 10));
            var backgrounds = BackstorySolver.TryToSolveWith(situation, variation);
            var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, situation);
            ProcGen.LastResults = finalSkills;
            for (var i = 0; i < pawnCount; i++)
            {
                var backstory = backgrounds[i];
                var forcedTraits = backstory.Background.Traits;
                if (!(finalSkills[i] is { } finalization))
                    continue;
                
                using (TemporarilyChange.ScenarioBannedTraits(empty))
                using (TemporarilyChange.PlayerFactionMelaninRange(ProcGen.MelaninRange))
                using (TemporarilyChange.BiologicalAgeRangeInRequest(ProcGen.AgeRange, i))
                using (TemporarilyChange.GenderInRequest(backstory.Background.Gender, i))
                {
                    pawnList[i] = StartingPawnUtility.RandomizeInPlace(pawnList[i]);
                }

                forcedTraits.ApplyRequestedTraitsTo(pawnList[i]);
                backstory.Background.ApplyBackstoryTo(pawnList[i]);
                finalization.ApplySimulatedSkillsTo(pawnList[i]);
                ProcGen.OnPawnChanged(pawnList[i]);
            }
        }

        public virtual Pawn RandomizeSingularPawn(Pawn pawn, CollectSpecificPassions collector,
            List<(SkillDef Skill, UsabilityRequirement Usability)> reqs)
        {
            var index = StartingPawnUtility.PawnIndex(pawn);
            var backstoryCategory = Faction.OfPlayer.def.backstoryFilters.First().categories.First();
            var specifier = new SelectBackstorySpecifically(backstoryCategory);
            var bio = specifier.GetBestBio(collector.Weight, ProcGen.TraitRequirements[index]);
            var traits = bio.Traits;
            var empty = new List<TraitDef>();

            var builder = new PawnBuilder(bio);
            foreach (var (skill, usability) in reqs.OrderBy(x => x.Usability).ThenByDescending(x => x.Skill.listOrder))
            {
                if (usability == UsabilityRequirement.Major)
                    builder.TryLockInPassion(skill, Passion.Major);
                else if (usability == UsabilityRequirement.Minor)
                    builder.TryLockInPassion(skill, Passion.Minor);
            }

            var addBackToLocked = false;
            if (ProcGen.LockedPawns.Contains(pawn))
            {
                ProcGen.LockedPawns.Remove(pawn);
                addBackToLocked = true;
            }

            using (TemporarilyChange.ScenarioBannedTraits(empty))
            using (TemporarilyChange.PlayerFactionMelaninRange(ProcGen.MelaninRange))
            using (TemporarilyChange.BiologicalAgeRangeInRequest(ProcGen.AgeRange, index))
            using (TemporarilyChange.GenderInRequest(bio.Gender, index))
            {
                pawn = StartingPawnUtility.RandomizeInPlace(pawn);
                ProcGen.OnPawnChanged(pawn);
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
    }
}
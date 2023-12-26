using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public static class ProcGen
    {
        private static IntRange ageRange = new IntRange(21, 30);
        private static float skillWeightVariation = 1.5f;
        private static FloatRange melaninRange = new FloatRange(0.0f, 0.9f);
        private static float maxPassionPoints = 7.0f;
        
        internal static List<List<TraitRequirement>> TraitRequirements { get; set; }
        internal static List<SkillPassionSelection> SkillPassions { get; set; }
        internal static List<Pawn> StartingPawns { get; set; }
        internal static IReadOnlyList<SkillFinalizationResult?> LastResults { get; private set; }
        internal static HashSet<Pawn> LockedPawns { get; } = new HashSet<Pawn>();

        internal static IntRange AgeRange
        {
            get => ageRange;
            set
            {
                if (ageRange != value)
                {
                    ageRange = value;
                    Dirty = true;
                }
            }
        }
        
        internal static IntRange AllowedAgeRange { get; set; }

        internal static float SkillWeightVariation
        {
            get => skillWeightVariation;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (skillWeightVariation != value)
                {
                    skillWeightVariation = value;
                    Dirty = true;
                }
            }
        }

        internal static FloatRange MelaninRange
        {
            get => melaninRange;
            set
            {
                if (melaninRange != value)
                {
                    melaninRange = value;
                    Dirty = true;
                }
            }
        }

        internal static float MaxPassionPoints
        {
            get => maxPassionPoints;
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (maxPassionPoints != value)
                {
                    maxPassionPoints = value;
                    Dirty = true;
                }
            }
        }

        internal static bool Dirty { get; private set; }

        public static void MakeDirty() => Dirty = true;

        public static void Generate(BalancingSituation situation)
        {
            var pawnList = Find.GameInitData.startingAndOptionalPawns;
            var pawnCount = Find.GameInitData.startingPawnCount;

            var empty = new List<TraitDef>();

            var variation = new IntRange(10, (int)(SkillWeightVariation * 10));
            var backgrounds = BackstorySolver.TryToSolveWith(situation, variation);
            var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, situation);
            LastResults = finalSkills;
            for (var i = 0; i < pawnCount; i++)
            {
                var backstory = backgrounds[i];
                var forcedTraits = backstory.Background.Traits;
                if (!(finalSkills[i] is { } finalization))
                    continue;
                
                using (TemporarilyChange.ScenarioBannedTraits(empty))
                using (TemporarilyChange.PlayerFactionMelaninRange(MelaninRange))
                using (TemporarilyChange.BiologicalAgeRange(AgeRange, i))
                using (TemporarilyChange.RequestedGender(backstory.Background.Gender, i))
                {
                    pawnList[i] = StartingPawnUtility.RandomizeInPlace(pawnList[i]);
                }

                forcedTraits.ApplyRequestedTraitsTo(pawnList[i]);
                backstory.Background.ApplyBackstoryTo(pawnList[i]);
                finalization.ApplySimulatedSkillsTo(pawnList[i]);
                OnPawnChanged(pawnList[i]);
            }

            Dirty = false;
        }

        public static void OnPawnChanged(Pawn pawn)
        {
            foreach (var window in Find.WindowStack.Windows)
            {
                switch (window)
                {
                    case Page_ConfigureStartingPawns startingPage:
                        startingPage.SelectPawn(pawn);
                        break;
                    case Interface.Pages.PrepareProcedurally _:
                        var index = StartingPawnUtility.PawnIndex(pawn);
                        StartingPawns[index] = pawn;
                        break;
                }
            }
        }

        public static void CleanUpOnError()
        {
            LockedPawns.Clear();
            var startingPawns = Find.GameInitData.startingAndOptionalPawns;
            for (var i = 0; i < startingPawns.Count; i++)
            {
                startingPawns[i] = StartingPawnUtility.RandomizeInPlace(startingPawns[i]);
                OnPawnChanged(startingPawns[i]);
            }
        }
        
        /// <summary>
        /// Set up a clean state based on starting scenario, map tile location, and ideology.
        /// </summary>
        public static void SetCleanState()
        {
            SkillPassions = DefDatabase<SkillDef>.AllDefsListForReading
                .Select(SkillPassionSelection.CreateFromSkill).ToList();
            var pawnCount = Find.GameInitData.startingPawnCount;
            StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
            TraitRequirements = StartingPawns.Select(x => new List<TraitRequirement>()).ToList();

            var kind = Faction.OfPlayer.def.basicMemberKind;
            int minimumAdulthoodAge = HumanoidAlienRaceCompatibility.IsHumanoidAlienRaceThingDef(kind.race)
                ? HumanoidAlienRaceCompatibility.GetAgeForAdulthoodBackstories(kind)
                : 20;
            
            int maximumAdulthoodAge = (int)kind.race.race.ageGenerationCurve.Last().x;

            AllowedAgeRange = new IntRange(minimumAdulthoodAge, maximumAdulthoodAge);
            ageRange = new IntRange(minimumAdulthoodAge + 1, Math.Min(maximumAdulthoodAge, minimumAdulthoodAge + 9));
            skillWeightVariation = 1.5f;
            // TODO: For tribal starts it might be fun to have this be based on latitude of the starting location.
            melaninRange = new FloatRange(0.0f, 0.9f);
            maxPassionPoints = 7.0f;

            // initial state will be clean so that any pawns that players want to keep from base randomization
            // won't be swept away by the PrepareProcedurally page automatically updating.
            Dirty = false;
        }
        
        /// <summary>
        /// Clear out any state that would result in Pawns being held onto.
        /// </summary>
        public static void ClearState()
        {
            LastResults = null;
            StartingPawns = null;
            LockedPawns.Clear();
        }
    }
}
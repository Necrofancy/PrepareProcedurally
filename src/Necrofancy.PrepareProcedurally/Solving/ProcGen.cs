using System.Collections.Generic;
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
                using (TemporarilyChange.RaceAgeGenerationCurve(AgeRange))
                {
                    pawnList[i] = StartingPawnUtility.RandomizeInPlace(pawnList[i]);
                    TraitUtilities.AddForcedTraits(pawnList[i], forcedTraits);
                }

                OnPawnChanged(pawnList[i]);
                backstory.Background.ApplyTo(pawnList[i]);
                finalization.ApplyTo(pawnList[i]);
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
    }
}
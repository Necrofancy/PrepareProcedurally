using System;
using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.Skills;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving
{
    public class ProcGen
    {        
        private static IntRange _ageRange = new IntRange(21, 30);
        private static float _skillWeightVariation = 1.5f;
        private static FloatRange _melaninRange = new FloatRange(0.0f, 0.9f);
        private static float _maxPassionPoints = 7.0f;
        
        internal static List<List<TraitRequirement>> TraitRequirements { get; set; }
        internal static List<SkillPassionSelection> SkillPassions { get; set; }
        internal static List<Pawn> StartingPawns { get; set; }
        internal static IReadOnlyList<SkillFinalizationResult?> LastResults { get; private set; }
        internal static HashSet<Pawn> LockedPawns { get; } = new HashSet<Pawn>();

        internal static IntRange AgeRange
        {
            get => _ageRange;
            set
            {
                if (_ageRange != value)
                {
                    _ageRange = value;
                    Dirty = true;
                }
            }
        }

        internal static float SkillWeightVariation
        {
            get => _skillWeightVariation;
            set
            {
                if (_skillWeightVariation != value)
                {
                    _skillWeightVariation = value;
                    Dirty = true;
                }
            }
        }

        internal static FloatRange MelaninRange
        {
            get => _melaninRange;
            set
            {
                if (_melaninRange != value)
                {
                    _melaninRange = value;
                    Dirty = true;
                }
            }
        }

        internal static float MaxPassionPoints
        {
            get => _maxPassionPoints;
            set
            {
                if (_maxPassionPoints != value)
                {
                    _maxPassionPoints = value;
                    Dirty = true;
                }
            }
        }

        internal static bool Dirty { get; private set; }

        public static void MakeDirty() => Dirty = true;

        public static void Generate(BalancingSituation situation)
        {
            var pawnList = Find.GameInitData.startingAndOptionalPawns;
            int pawnCount = Find.GameInitData.startingPawnCount;

            var empty = new List<TraitDef>();

            var skillWeightVariation = new IntRange(10, (int)(ProcGen.SkillWeightVariation * 10));
            var backgrounds = BackstorySolver.TryToSolveWith(situation, skillWeightVariation);
            var finalSkills = BackstorySolver.FigureOutPassions(backgrounds, situation);
            LastResults = finalSkills;
            for (int i = 0; i < pawnCount; i++)
            {
                var backstory = backgrounds[i];
                var forcedTraits = backstory.Background.Traits;
                if (!(finalSkills[i] is { } finalization))
                    continue;
                
                using (NarrowBioEditor.RestrictTraits(forcedTraits, empty))
                using (NarrowBioEditor.MelaninRange(MelaninRange.min, MelaninRange.max))
                using (NarrowBioEditor.FilterPawnAges(AgeRange.min, AgeRange.max))
                using (NarrowBioEditor.FilterRequestAge(i, ProcGen.AgeRange.min, ProcGen.AgeRange.max))
                {
                    pawnList[i] = StartingPawnUtility.RandomizeInPlace(pawnList[i]);
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
    }
}
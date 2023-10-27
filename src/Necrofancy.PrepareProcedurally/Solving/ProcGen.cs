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
        internal static IReadOnlyList<SkillFinalizationResult?> LastResults { get; private set; }
        internal static List<SkillPassionSelection> skillPassions;
        internal static List<Pawn> startingPawns;
        internal static HashSet<Pawn> LockedPawns { get; } = new HashSet<Pawn>();
        internal static IntRange AgeRange { get; set; } = new IntRange(21, 30);
        internal static FloatRange MelaninRange { get; set; } = new FloatRange(0.75f, 0.9f);
        internal static IntRange JobVariation { get; set; } = new IntRange(8, 12);

        public static void Generate(BalancingSituation situation)
        {
            var pawnList = Find.GameInitData.startingAndOptionalPawns;
            int pawnCount = Find.GameInitData.startingPawnCount;

            var empty = new List<TraitDef>();
            
            var backgrounds = BackstorySolver.TryToSolveWith(situation, JobVariation);
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
                        startingPawns[index] = pawn;
                        break;
                }
            }
        }
    }
}
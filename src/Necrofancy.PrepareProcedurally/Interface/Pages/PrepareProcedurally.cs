
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Editor;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using UnityEngine;
using Verse;
using PawnTableDefOf = Necrofancy.PrepareProcedurally.Defs.PawnTableDefOf;

namespace Necrofancy.PrepareProcedurally.Interface.Pages
{
    public class PrepareProcedurally : Page
    {
        public PrepareProcedurally()
        {
            this.closeOnClickedOutside = true;
            
            int pawnCount = Find.GameInitData.startingPawnCount;
            ProcGen.StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
        }

        public override string PageTitle => "SkillPassionPageTitle".Translate();

        public override void DoWindowContents(Rect rect)
        {
            this.DrawPageTitle(rect);

            if (ProcGen.SkillPassions is null)
            {
                SetDefaultState();
            }

            var uiPadding = rect.GetInnerRect();
            uiPadding.SplitHorizontally(480, out var upper, out var lower);

            SkillPassionSelectionUiUtility.DoWindowContents(upper, ProcGen.SkillPassions);
            this.PropagateToEditor();
            
            var pawnTable = new MaplessPawnTable(PawnTableDefOf.PrepareProcedurallyResults, GetStartingPawns, 400, 700);
            pawnTable.SetMinMaxSize(400, (int)lower.width, 500, (int)lower.height);
            pawnTable.PawnTableOnGUI(lower.min);
        }

        private static IEnumerable<Pawn> GetStartingPawns()
        {
            return Find.GameInitData.startingAndOptionalPawns.Take(Find.GameInitData.startingPawnCount);
        }

        public static void SetDefaultState()
        {
            ProcGen.SkillPassions = DefDatabase<SkillDef>.AllDefsListForReading
                .Select(SkillPassionSelection.CreateFromSkill).ToList();
            int pawnCount = Find.GameInitData.startingPawnCount;
            ProcGen.StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
            ProcGen.TraitRequirements = ProcGen.StartingPawns.Select(x => new List<TraitRequirement>()).ToList();
        }

        private void PropagateToEditor()
        {
            if (ProcGen.Dirty)
            {
                // close existing windows
                while (Find.WindowStack.WindowOfType<EditSpecificPawn>() is { } editSpecificPawn)
                {
                    editSpecificPawn.Close();
                }

                string backstoryCategory = Faction.OfPlayer.def.backstoryFilters.First().categories.First();
                int pawnCount = Find.GameInitData.startingPawnCount;
                var situation = new BalancingSituation(string.Empty, backstoryCategory, pawnCount, ProcGen.SkillPassions);
                
                ProcGen.Generate(situation);
                
                ProcGen.StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
            }
        }
    }
}
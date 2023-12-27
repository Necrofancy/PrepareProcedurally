
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
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
            var pawnCount = Find.GameInitData.startingPawnCount;
            Editor.StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
        }

        public override string PageTitle => "SkillPassionPageTitle".Translate();

        public override void DoWindowContents(Rect rect)
        {
            try
            {
                DrawPageTitle(rect);

                var uiPadding = rect.GetInnerRect();
                uiPadding.SplitHorizontally(480, out var upper, out var lower);

                SkillPassionSelectionUiUtility.DoWindowContents(upper, Editor.SkillPassions);

                var pawnTable = new MaplessPawnTable(PawnTableDefOf.PrepareProcedurallyResults, GetStartingPawns, 400,
                    800);
                pawnTable.SetMinMaxSize(400, (int)lower.width, 700, (int)lower.height + 200);
                lower.y -= 8.6f * Find.GameInitData.startingPawnCount;
                Text.Font = GameFont.Tiny;
                Widgets.Label(lower, "ClickOnPawnToCustomizeSkills".Translate());
                pawnTable.PawnTableOnGUI(lower.min);

                PropagateToEditor();

                DoBottomButtons(rect);
            }
            catch (Exception e)
            {
                Log.Error($"Exception In Workflow of Prepare Procedurally - Closing Window\n{e}");

                ProcGen.CleanUpOnError();
                this.FullyClose();
                string errorMessage = "PrepareProcedurallyErrorMessage".Translate(e.Message);
                var window = new Dialog_MessageBox(errorMessage);
                Find.WindowStack.Add(window);
            }
        }

        private static IEnumerable<Pawn> GetStartingPawns()
        {
            return Find.GameInitData.startingAndOptionalPawns.Take(Find.GameInitData.startingPawnCount);
        }

        protected override void DoNext()
        {
            List<SkillDef> unsatisfiedRequirements = new List<SkillDef>();
            foreach (var requirement in Editor.SkillPassions)
            {
                if (!requirement.StartingGroupSatisfies(Editor.StartingPawns))
                    unsatisfiedRequirements.Add(requirement.Skill);
            }
            
            if (unsatisfiedRequirements.Any())
            {
                StringBuilder builder = new StringBuilder();
                foreach (var unsatisfied in unsatisfiedRequirements)
                {
                    builder.Append("    - ");
                    builder.AppendLine(unsatisfied.LabelCap);
                }

                var message = "WarnUnsatisfiedSkills".Translate(builder.ToString());
                var window = new Dialog_MessageBox(message, "Yes".Translate(), FullyClose, "No".Translate());
                Find.WindowStack.Add(window);
            }
            else
            {
                FullyClose();
            }
        }

        public override bool OnCloseRequest()
        {
            CloseSubdialogs();
            return base.OnCloseRequest();
        }

        private static void CloseSubdialogs()
        {
            while (Find.WindowStack.WindowOfType<EditSpecificPawn>() is { } editSpecificPawn)
            {
                editSpecificPawn.Close(doCloseSound: false);
            }
        }

        private void FullyClose()
        {
            CloseSubdialogs();
            Close();
        }

        private void PropagateToEditor()
        {
            if (Editor.Dirty)
            {
                CloseSubdialogs();
                
                var backstoryCategory = Faction.OfPlayer.def.backstoryFilters.First().categories.First();
                var pawnCount = Find.GameInitData.startingPawnCount;
                var situation = new BalancingSituation(string.Empty, backstoryCategory, pawnCount, Editor.SkillPassions);
                
                ProcGen.Generate(situation);
                
                Editor.StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
            }

            Editor.AllowDirtying = true;
        }
    }
}
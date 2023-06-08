
using System.Linq;
using Necrofancy.PrepareProcedurally.Editor;
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
            ProcGen.startingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
        }

        public override string PageTitle => "SkillPassionPageTitle".Translate();

        public override void DoWindowContents(Rect rect)
        {
            this.DrawPageTitle(rect);

            if (ProcGen.skillPassions is null)
            {
                SetDefaultState();
            }

            var uiPadding = rect.ContractedBy(20, 60);
            uiPadding.SplitHorizontally(480, out var upper, out var lower);
            if (SkillPassionSelectionUiUtility.DoWindowContents(upper, ProcGen.skillPassions))
            {
                this.PropagateToEditor();
            }
            
            var pawnTable = new MaplessPawnTable(PawnTableDefOf.PrepareProcedurallyResults, () => ProcGen.startingPawns, 400, 500);
            pawnTable.SetMinMaxSize(400, (int)lower.width, 500, (int)lower.height);
            pawnTable.PawnTableOnGUI(lower.min);
        }

        public static void SetDefaultState()
        {
            ProcGen.skillPassions = DefDatabase<SkillDef>.AllDefsListForReading
                .Select(SkillPassionSelection.CreateFromSkill).ToList();
            int pawnCount = Find.GameInitData.startingPawnCount;
            ProcGen.startingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
        }

        private void PropagateToEditor()
        {
            string backstoryCategory = Faction.OfPlayer.def.backstoryFilters.First().categories.First();
            int pawnCount = Find.GameInitData.startingPawnCount;
            var situation = new BalancingSituation(string.Empty, backstoryCategory, pawnCount, ProcGen.skillPassions);

            ProcGen.Generate(situation);
            
            ProcGen.startingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();
        }
    }
}
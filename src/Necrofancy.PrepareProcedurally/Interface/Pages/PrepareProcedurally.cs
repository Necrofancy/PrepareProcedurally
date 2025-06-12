using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using PawnTableDefOf = Necrofancy.PrepareProcedurally.Defs.PawnTableDefOf;

namespace Necrofancy.PrepareProcedurally.Interface.Pages;

public class PrepareProcedurally : Page
{
    private const string CustomizePawnSkillsLabel = "Necrofancy.PrepareProcedurally.CustomizePawnSkillsLabel";
    private const string RerollButton = "Necrofancy.PrepareProcedurally.RerollButton";

    private const string PrepareProcedurallyErrorMessage =
        "Necrofancy.PrepareProcedurally.PrepareProcedurallyErrorMessage";

    private const string WarnUnsatisfiedSkillsMessage = "Necrofancy.PrepareProcedurally.WarnUnsatisfiedSkillsMessage";
    public override string PageTitle => "Necrofancy.PrepareProcedurally.PageTitle".Translate();

    private static Vector2 scrollPosition;

    public PrepareProcedurally()
    {
        // Just in case pawns were randomized outside of the editor, refresh pawn editor.
        Editor.RefreshPawnList();
    }

    public override void DoWindowContents(Rect rect)
    {
        try
        {
            DrawPageTitle(rect);

            var uiPadding = rect.GetInnerRect();
            uiPadding.SplitHorizontally(400, out var upper, out var lower);

            SkillPassionSelectionUiUtility.DoWindowContents(upper, Editor.SkillPassions);

            Text.Font = GameFont.Tiny;
            Widgets.Label(lower, CustomizePawnSkillsLabel.Translate());

            var table = new MaplessPawnTable(PawnTableDefOf.PrepareProcedurallyResults, GetStartingPawns, (int)rect.width,
                800);
            lower.y += 15;
            var viewSize = new Rect(lower.position, table.Size)
            {
                width = lower.width - 50
            };
            lower.height -= 45;

            Widgets.BeginScrollView(lower, ref scrollPosition, viewSize);
            table.PawnTableOnGUI(lower.min);
            Widgets.EndScrollView();

            PropagateToEditor();

            void Reroll()
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                Editor.MakeDirty();
            }

            DoBottomButtons(rect, midLabel: RerollButton.Translate(), midAct: Reroll);
        }
        catch (Exception e)
        {
            Close();
            Log.Error($"Exception In Workflow of Prepare Procedurally - Closing Window\n{e}");
            ProcGen.CleanUpOnError();
            string errorMessage = PrepareProcedurallyErrorMessage.Translate(e.Message);
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
        var unsatisfiedRequirements =
            new List<(SkillDef Skill, int MajorMissing, int MinorMissing, int UsableMissing)>();
        foreach (var requirement in Editor.SkillPassions)
            if (!requirement.StartingGroupSatisfies(Editor.StartingPawns, out var major, out var minor, out var usable))
                unsatisfiedRequirements.Add((requirement.Skill, major, minor, usable));

        if (unsatisfiedRequirements.Any())
        {
            var builder = new StringBuilder();
            foreach (var unsatisfied in unsatisfiedRequirements)
            {
                builder.Append("    - ");
                builder.AppendLine(unsatisfied.Skill.LabelCap);
                if (unsatisfied.MajorMissing > 0)
                    builder.AppendLine($"        - Major: {unsatisfied.MajorMissing}");
                if (unsatisfied.MinorMissing > 0)
                    builder.AppendLine($"        - Minor: {unsatisfied.MinorMissing}");
                if (unsatisfied.UsableMissing > 0)
                    builder.AppendLine($"        - Usable: {unsatisfied.UsableMissing}");
            }

            var message = WarnUnsatisfiedSkillsMessage.Translate(builder.ToString());
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
        while (Find.WindowStack.WindowOfType<EditSpecificPawn>() is { } editSpecificPawn) editSpecificPawn.Close(false);
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

            var backstoryFilters = Faction.OfPlayer.def.backstoryFilters;
            var pawnCount = Find.GameInitData.startingPawnCount;
            var situation = new BalancingSituation(string.Empty, backstoryFilters, pawnCount, Editor.SkillPassions);

            Compatibility.Layer.RandomizeForTeam(situation);

            Editor.StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToList();

            Editor.Dirty = false;
        }

        Editor.AllowDirtying = true;
    }
}
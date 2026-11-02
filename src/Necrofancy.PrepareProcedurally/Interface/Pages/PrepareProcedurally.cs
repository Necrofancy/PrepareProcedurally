using System;
using System.Collections.Generic;
using System.Diagnostics;
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
#if DEBUG
    int stressTestModeRuns, requirementSuccesses;
#endif
    private const string CustomizePawnSkillsLabel = "Necrofancy.PrepareProcedurally.CustomizePawnSkillsLabel";
    private const string RerollButton = "Necrofancy.PrepareProcedurally.RerollButton";

    private const string PrepareProcedurallyErrorMessage =
        "Necrofancy.PrepareProcedurally.PrepareProcedurallyErrorMessage";

    private const string WarnUnsatisfiedSkillsMessage = "Necrofancy.PrepareProcedurally.WarnUnsatisfiedSkillsMessage";
    public override string PageTitle => "Necrofancy.PrepareProcedurally.PageTitle".Translate();

    private static Vector2 scrollPosition;

    public PrepareProcedurally()
    {
        Editor.RefreshPawnList();
        Editor.Dirty = PrepareMod.Settings.preGenerate;
    }

    public override void DoWindowContents(Rect rect)
    {
        try
        {
            DrawPageTitle(rect);
            DrawStressTestCheckmark(rect);
            
            var uiPadding = rect.GetInnerRect();
            uiPadding.SplitHorizontally(400, out var upper, out var lower);

            var leftWidth = rect.width - SettingsUiUtility.OverallWidth;
            rect.SplitVertically(leftWidth, out var textRect, out var skillSelectRect);
            
            textRect.y += 24;
            skillSelectRect.y -= 18;
            
            SettingsUiUtility.ForEditor.DoWindowContents(textRect);
            SkillPassionSelectionUiUtility.DoWindowContents(skillSelectRect, Editor.SkillPassions);

            Text.Font = GameFont.Tiny;
            Widgets.Label(lower, CustomizePawnSkillsLabel.Translate());

            var table = new MaplessPawnTable(PawnTableDefOf.PrepareProcedurallyResults, GetStartingPawns, (int)rect.width,
                800);
            lower.y += 15;
            var viewSize = new Rect(lower.position, table.Size)
            {
                width = lower.width - 50
            };
            
            DrawStressTestOngoingResults(lower);
            lower.height -= 45;

            Widgets.BeginScrollView(lower, ref scrollPosition, viewSize);
            table.PawnTableOnGUI(lower.min);
            Widgets.EndScrollView();

            PropagateToEditor();

            void Reroll()
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                RandomizeForTeam();
            }

            DoBottomButtons(rect);
            DoCenterButtons(rect, Reroll);
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

    [Conditional("DEBUG")]
    private void DrawStressTestCheckmark(Rect rect)
    {
#if DEBUG
        // stres test mode will repeatedly roll randomization on every call of DrawWindowContents
        const float checkboxSpace = 30f;
        var labelText =  "Stress Test Mode";
        var rectLength = Text.CalcSize(labelText).x + checkboxSpace;

        bool stressTesting = Editor.StressTestMode;
        Rect soloRect = new Rect(rect.xMax - rectLength, rect.y - 5, rectLength, Text.LineHeight);
        Widgets.CheckboxLabeled(soloRect, labelText, ref stressTesting);
        Editor.StressTestMode = stressTesting;
#endif
    }

    [Conditional("DEBUG")]
    private void DrawStressTestOngoingResults(Rect rect)
    {
#if DEBUG
        if (!Editor.StressTestMode && stressTestModeRuns <= 0)
        {
            return;
        }
        
        float rate = (float)requirementSuccesses / stressTestModeRuns;
        Widgets.Label(rect, $"{stressTestModeRuns} runs, ({rate:P2} success)");
        
        if (Editor.StressTestMode)
        {
            var requirementsUnmet = GetUnsatisfiedRequirements();
            if (!requirementsUnmet.Any())
                requirementSuccesses++;
            stressTestModeRuns++;
        }
#endif
    }

    private void DoCenterButtons(Rect rect, Action reroll)
    {
        const float gap = 16f;
        Text.Font = GameFont.Small;

        var size = BottomButSize;
        var startX = rect.x + (rect.width - (size.x * 2f + gap)) / 2f;
        var y = rect.yMax - size.y;

        var settingsRect = new Rect(startX, y, size.x, size.y);
        var rerollRect = new Rect(startX + size.x + gap, y, size.x, size.y);

        if (Widgets.ButtonText(settingsRect, "ManageModSettings".PpTranslate()))
            Find.WindowStack.Add(new PrepareSettingsDialog());

        if (Widgets.ButtonText(rerollRect, RerollButton.Translate()))
            reroll();
    }

    private static IEnumerable<Pawn> GetStartingPawns()
    {
        return Find.GameInitData.startingAndOptionalPawns.Take(Find.GameInitData.startingPawnCount);
    }

    protected override void DoNext()
    {
        var unsatisfiedRequirements = GetUnsatisfiedRequirements();

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

    private static List<(SkillDef Skill, int MajorMissing, int MinorMissing, int UsableMissing)> GetUnsatisfiedRequirements()
    {
        var unsatisfiedRequirements =
            new List<(SkillDef Skill, int MajorMissing, int MinorMissing, int UsableMissing)>();
        foreach (var requirement in Editor.SkillPassions)
            if (!requirement.StartingGroupSatisfies(Editor.StartingPawns, out var major, out var minor, out var usable))
                unsatisfiedRequirements.Add((requirement.Skill, major, minor, usable));
        return unsatisfiedRequirements;
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
        if (Editor.ReadyForEdit)
        {
            CloseSubdialogs();
            RandomizeForTeam();
        }
    }

    private void RandomizeForTeam()
    {
        var backstoryFilters = Faction.OfPlayer.def.backstoryFilters;
        var pawnCount = Find.GameInitData.startingPawnCount;
        var situation = new BalancingSituation(string.Empty, backstoryFilters, pawnCount, Editor.SkillPassions);

        // Compatibility layers (e.g. HAR) index into Editor.StartingPawns and pass those pawns to
        // StartingPawnUtility.RandomizeInPlace, which throws if a pawn is no longer in GameInitData.
        Editor.RefreshPawnList();
        Compatibility.Layer.RandomizeForTeam(situation);

        Editor.StartingPawns = Find.GameInitData.startingAndOptionalPawns.Take(pawnCount).ToArray();

        Editor.Dirty = false;
    }
    
    private static int GetOverallRowUiLength()
    {
        return int.Parse("Necrofancy.PrepareProcedurally.SelectSkillWidgetLength".Translate());
    }
}
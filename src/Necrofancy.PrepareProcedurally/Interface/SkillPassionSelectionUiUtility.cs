using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Necrofancy.PrepareProcedurally.Interface.UiAdjustmentScope;

namespace Necrofancy.PrepareProcedurally.Interface;

/// <summary>
/// UI Utilities to have a drop down of skills and work potential.
/// </summary>
/// <remarks>
/// This is based on the Page_CreateWorldParams and repurposed towards supporting skills for passions in a skill.
/// </remarks>
public static class SkillPassionSelectionUiUtility
{
    private const string SkillSelectWidgetLabel = "Necrofancy.PrepareProcedurally.SkillsGroupLabel";
    private const string UsableText = "Necrofancy.PrepareProcedurally.CapableOf";
    
    private static readonly Lazy<int> SkillTitleLength = new(GetSkillTitleColumnLength);

    private const int PlusMinusButtonWidth = 5;
    private const string Plus = "+";
    private const string Minus = "-";

    private static float listingHeight;

    private const float RowHeight = 24f;
    private const float AddButtonHeight = 28f;

    private const float RowMarginX = 6f;

    // WidgetRow.Button will add a constant value to the fixed width of a button. We have to account for this.
    private const int GapBetweenKnobs = 9;

    private const int NumericLabelTextLength = 10;

    // These are different to even balance 
    private const int GapBeforeNumeric = -3;
    private const int GapAfterNumeric = -2;
    
    public static void DoWindowContents(Rect skillSelectRect, List<SkillPassionSelection> skillPassions)
    {
        var lineHeight = new Rect(skillSelectRect.x + 20f, skillSelectRect.y, skillSelectRect.width, Text.LineHeight);
        Widgets.Label(lineHeight, SkillSelectWidgetLabel.Translate());

        var num1 = Text.LineHeight + 4f;
        var num2 = skillSelectRect.width * 0.050000012f;
        var rect2 = new Rect(skillSelectRect.x + num2, skillSelectRect.y + num1, skillSelectRect.width * 0.9f,
            (float)(skillSelectRect.height - (double)num1 - Text.LineHeight - 28.0));
        var outRect = rect2.ContractedBy(4f);
        var rect3 = new Rect(outRect.x, outRect.y, outRect.width, listingHeight);
        listingHeight = 0.0f;

        var listingStandard = new Listing_Standard { ColumnWidth = rect3.width };
        listingStandard.Begin(rect3);
        for (var index = 0; index < skillPassions.Count; ++index)
        {
            listingStandard.Gap(4f);
            if (DoRow(listingStandard.GetRect(RowHeight), skillPassions[index], skillPassions, index))
                --index;
            listingStandard.Gap(4f);
            listingHeight += 32f;
        }

        listingStandard.End();

        //now we add the buttons

        var options = new List<FloatMenuOption>();

        bool NotInExistingList(SkillDef def)
        {
            return !skillPassions.Any(sp => sp.Skill == def);
        }

        foreach (var skillDef in DefDatabase<SkillDef>.AllDefsListForReading.Where(NotInExistingList))
        {
            var str = skillDef.skillLabel;

            void Add()
            {
                skillPassions.Add(SkillPassionSelection.CreateFromSkill(skillDef));
                skillPassions.SortByDescending(x => x.Skill.listOrder);
            }

            options.Add(new FloatMenuOption(str, Add));
        }

        if (options.Any())
        {
            var rect4 = new Rect(outRect.x,
                Mathf.Min(rect2.yMax, (float)(outRect.y + (double)listingHeight + 4.0)), outRect.width,
                AddButtonHeight);
            if (Widgets.ButtonText(rect4, "Add".Translate().CapitalizeFirst() + "..."))
                Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private static bool DoRow(
        Rect rect,
        SkillPassionSelection selection,
        List<SkillPassionSelection> factions,
        int index)
    {
        var pawnCount = Find.GameInitData.startingPawnCount;
        var pawnsRemaining = pawnCount - selection.major - selection.minor - selection.usable;
        var flag = false;
        var rect1 = new Rect(rect.x, rect.y - 4f, rect.width, rect.height + 8f);
        if (index % 2 == 1)
            Widgets.DrawLightHighlight(rect1);
        Widgets.BeginGroup(rect);

        var widgetRow = new WidgetRow(RowMarginX, 0.0f);
        widgetRow.Gap(4f);
        using (ForegroundColorOf(Color.white))
        using (TextAnchorOf(TextAnchor.MiddleLeft))
        {
            using (TextAnchorOf(TextAnchor.MiddleRight))
            {
                widgetRow.Label(selection.Skill.LabelCap, SkillTitleLength.Value);
            }

            // draw major passions selection section
            widgetRow.Gap(GapBetweenKnobs);
            widgetRow.Icon(SkillUI.PassionMajorIcon);
            widgetRow.Gap(-5);

            if (ButtonHit(widgetRow, Minus, selection.major > 0))
            {
                selection.major--;
                Editor.MakeDirty();
            }

            widgetRow.Gap(GapBeforeNumeric);
            widgetRow.Label(selection.major.ToString(), NumericLabelTextLength);
            widgetRow.Gap(GapAfterNumeric);

            if (ButtonHit(widgetRow, Plus, pawnCount > selection.major))
            {
                if (pawnsRemaining <= 0 && selection.usable > 0)
                    selection.usable--;
                else if (pawnsRemaining <= 0 && selection.minor > 0)
                    selection.minor--;

                selection.major++;
                Editor.MakeDirty();
            }

            // draw minor passions selection section if we can give a minor passion role.
            var canAddOrRemovePassions = pawnCount > selection.major;
            if (!canAddOrRemovePassions) GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);

            widgetRow.Gap(GapBetweenKnobs);
            widgetRow.Icon(SkillUI.PassionMinorIcon);
            widgetRow.Gap(-9);

            if (ButtonHit(widgetRow, Minus, selection.minor > 0))
            {
                selection.minor--;
                Editor.MakeDirty();
            }

            widgetRow.Gap(GapBeforeNumeric);
            widgetRow.Label(selection.minor.ToString(), NumericLabelTextLength);
            widgetRow.Gap(GapAfterNumeric);

            if (ButtonHit(widgetRow, Plus, pawnCount > selection.major + selection.minor))
            {
                if (pawnsRemaining <= 0 && selection.usable > 0)
                    selection.usable--;

                selection.minor++;
                Editor.MakeDirty();
            }

            // draw minimal non-passion selection section if more pawns can be assigned.
            var canInteractWithMinimalBar = pawnCount > selection.major + selection.minor;
            if (!canInteractWithMinimalBar) GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);

            widgetRow.Gap(GapBetweenKnobs);
            widgetRow.Label(UsableText.Translate());
            widgetRow.Gap(GapAfterNumeric);
            if (ButtonHit(widgetRow, Minus, selection.usable > 0))
            {
                selection.usable--;
                Editor.MakeDirty();
            }

            widgetRow.Gap(GapBeforeNumeric);
            widgetRow.Label(selection.usable.ToString(), NumericLabelTextLength);
            widgetRow.Gap(GapAfterNumeric);

            if (ButtonHit(widgetRow, Plus, pawnsRemaining > 0))
            {
                selection.usable++;
                Editor.MakeDirty();
            }
        }

#if RW1_4
        var delete = TexButton.DeleteX;
#else
        var delete = TexButton.Delete;
#endif
        if (Widgets.ButtonImage(new Rect((float)(rect.width - 24.0 - 6.0), 0.0f, 24f, 24f), delete))
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            factions.RemoveAt(index);
            flag = true;
            Editor.MakeDirty();
        }

        Widgets.EndGroup();

        if (Mouse.IsOver(rect1))
        {
            var builder = new StringBuilder();
            builder.AppendLine(selection.Skill.description.AsTipTitle()).AppendLine();
            builder.AppendLine("Necrofancy.PrepareProcedurally.SkillSettingsTooltip".Translate(selection.major,
                selection.minor, selection.usable,
                selection.Skill.LabelCap));
            TooltipHandler.TipRegion(rect1, (TipSignal)builder.ToString());
            Widgets.DrawHighlight(rect1);
        }

        return flag;
    }

    private static bool ButtonHit(WidgetRow row, string text, bool enabled)
    {
        if (enabled)
            return row.ButtonText(text, fixedWidth: PlusMinusButtonWidth);

        var rect = row.ButtonRect(text, PlusMinusButtonWidth);
        ButtonDisabled(rect, text);
        return false;
    }

    private static void ButtonDisabled(Rect rect, string label)
    {
        var lightGray = new Color(0.65f, 0.65f, 0.65f, 1f);

        using (TextAnchorOf(TextAnchor.MiddleCenter))
        using (TextWrapOf(false))
        using (BackgroundColorOf(lightGray))
        using (ForegroundColorOf(lightGray))
        {
            Widgets.DrawAtlas(rect, Widgets.ButtonBGAtlas);
            Widgets.Label(rect, label);
        }
    }

    private static int GetSkillTitleColumnLength()
    {
        return int.Parse("Necrofancy.PrepareProcedurally.SkillTextLength".Translate());
    }
}
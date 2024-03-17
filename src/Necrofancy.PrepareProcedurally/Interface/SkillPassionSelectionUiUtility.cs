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

    private const string PawnBiology = "Necrofancy.PrepareProcedurally.BiologicalSettingsLabel";
    private const string AgeRangeText = "Necrofancy.PrepareProcedurally.BiologicalAgeRange";
    private const string MelaninRangeText = "Necrofancy.PrepareProcedurally.BiologicalMelaninRange";
    private const string InjuriesLabel = "Necrofancy.PrepareProcedurally.BiologicalAllowInjuriesLabel";
    private const string InjuriesTooltip = "Necrofancy.PrepareProcedurally.BiologicalAllowInjuriesTooltip";
    private const string RelationshipsLabel = "Necrofancy.PrepareProcedurally.BiologicalAllowRelationshipsLabel";
    private const string RelationshipsTooltip = "Necrofancy.PrepareProcedurally.BiologicalAllowRelationshipsTooltip";
    private const string PregnancyLabel = "Necrofancy.PrepareProcedurally.BiologicalAllowPregnancyLabel";
    private const string PregnancyTooltip = "Necrofancy.PrepareProcedurally.BiologicalAllowPregnancyTooltip";
    private const string PassionText = "Necrofancy.PrepareProcedurally.BackstoryPassionLabel";
    private const string SkillVariationText = "Necrofancy.PrepareProcedurally.VariationLabel";
    private const string PassionMaxText = "Necrofancy.PrepareProcedurally.PassionPointsLabel";
    private const string PassionGroupText = "Necrofancy.PrepareProcedurally.GroupwideUsageIndicator";
    private const string SkillVariationTooltip = "Necrofancy.PrepareProcedurally.SkillVariationTooltip";
    private const string PassionPointsTooltip = "Necrofancy.PrepareProcedurally.PassionPointsTooltip";
    private const string SkillVariationLeft = "Necrofancy.PrepareProcedurally.SkillVariationLeft";
    private const string SkillVariationRight = "Necrofancy.PrepareProcedurally.SkillVariationRight";

    private static readonly Lazy<int> SkillTitleLength = new(GetSkillTitleColumnLength);
    private static readonly Lazy<int> OverallRowLength = new(GetOverallRowUiLength);

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

    private static IntRange melaninRange = new(0, PawnSkinColors.SkinColorGenesInOrder.Count - 1);

    public static void DoWindowContents(Rect rect, List<SkillPassionSelection> skillPassions)
    {
        var leftWidth = rect.width - OverallRowLength.Value;
        rect.SplitVertically(leftWidth, out var textRect, out var skillSelectRect);

        textRect.y += RowHeight;
        skillSelectRect.y -= 18;

        // set up biological page section
        var labelText = new Rect(textRect.x, textRect.y, textRect.width, RowHeight);
        labelText.SplitVertically(200f, out var labelRect, out var right);
        right = right.ContractedBy(2);
        Widgets.Label(labelRect, PawnBiology.Translate());
        if (Editor.RaceAgeRanges.Count > 1 && Widgets.ButtonText(right, Editor.SelectedRace.LabelCap))
        {
            var targets = Editor.RaceAgeRanges.Keys;

            var selectPawnKinds = new List<FloatMenuOption>();
            foreach (var option in targets)
            {
                var str = option.LabelCap;

                void Select()
                {
                    Editor.SelectedRace = option;
                }

                selectPawnKinds.Add(new FloatMenuOption(str, Select));
            }

            Find.WindowStack.Add(new FloatMenu(selectPawnKinds));
        }

        var bioCount = ModsConfig.BiotechActive ? 8 : 7;
        var bioRect = new Rect(textRect.x, textRect.y + RowHeight, textRect.width, RowHeight * bioCount);
        Widgets.DrawMenuSection(bioRect);
        var bioInnerRect = bioRect.GetInnerRect();

        // age slider
        var ageSlider = new Rect(bioInnerRect.x, bioInnerRect.y, bioInnerRect.width, RowHeight);
        var ageRange = Editor.AgeRange;

        // Age minimum is to force an adulthood backstory.
        var minAge = Editor.AllowedAgeRange.min;
        var maxAge = Editor.AllowedAgeRange.max;

        Widgets.IntRange(ageSlider, 1235, ref ageRange, minAge, maxAge, AgeRangeText, 4);
        Editor.AgeRange = ageRange;

        // melanin slider
        var melaninSlider = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 1.5f, bioInnerRect.width, RowHeight);
        var genes = PawnSkinColors.SkinColorGenesInOrder;
        var maxMelanin = genes.Count - 1;
        Widgets.IntRange(melaninSlider, 12345, ref melaninRange, 0, maxMelanin, MelaninRangeText, 1);
        var minSelectedMelanin = genes[melaninRange.min].minMelanin;
        var maxSelectedMelanin = melaninRange.max >= maxMelanin ? 1 : genes[melaninRange.max + 1].minMelanin;
        Editor.MelaninRange = new FloatRange(minSelectedMelanin, maxSelectedMelanin);
        
        // Allow Bad HeDiffs Checkbox
        var allowInjuries = Editor.AllowBadHeDiffs;
        var allowInjuriesRect = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 4f, bioInnerRect.width, RowHeight);
        Widgets.CheckboxLabeled(allowInjuriesRect, InjuriesLabel.Translate(), ref allowInjuries);
        TooltipHandler.TipRegion(allowInjuriesRect, InjuriesTooltip.Translate());
        Editor.AllowBadHeDiffs = allowInjuries;
        
        // Allow Relationships checkbox
        var allowRelationships = Editor.AllowRelationships;
        var allowRelationshipsRect = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 5f, bioInnerRect.width, RowHeight);
        Widgets.CheckboxLabeled(allowRelationshipsRect, RelationshipsLabel.Translate(), ref allowRelationships);
        TooltipHandler.TipRegion(allowRelationshipsRect, RelationshipsTooltip.Translate());
        Editor.AllowRelationships = allowRelationships;

        if (ModsConfig.BiotechActive)
        {
            var allowPregnant = Editor.AllowPregnancy;
            var allowPregnancyRect = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 6f, bioInnerRect.width, RowHeight);
            Widgets.CheckboxLabeled(allowPregnancyRect, PregnancyLabel.Translate(), ref allowPregnant);
            TooltipHandler.TipRegion(allowPregnancyRect, PregnancyTooltip.Translate());
            Editor.AllowPregnancy = allowPregnant;
        }

        var minSkinColor = genes[melaninRange.min].IconColor;
        var maxSkinColor = genes[melaninRange.max].IconColor;

        var midPoint = bioInnerRect.x + bioInnerRect.width / 2;
        const int size = 20;
        var rectLeft = new Rect(midPoint - size, bioInnerRect.y + RowHeight * 3f, size, size);
        var rectMid = new Rect(midPoint, bioInnerRect.y + RowHeight * 3f, size, size);
        var rectRight = new Rect(midPoint + size, bioInnerRect.y + RowHeight * 3f, size, size);

        Widgets.DrawRectFast(rectLeft, minSkinColor);
        Widgets.DrawRectFast(rectRight, maxSkinColor);
        GUI.color = minSkinColor;
        Widgets.DrawBox(rectLeft);
        GUI.color = maxSkinColor;
        Widgets.DrawBox(rectRight);
        GUI.color = new Color(0.6f, 0.6f, 0.6f);
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(rectMid, "-");
        Text.Anchor = TextAnchor.UpperLeft;

        GUI.color = Color.white;

        // label for text
        var skillLabels = new Rect(textRect.x, textRect.y + RowHeight * 10, textRect.width, RowHeight);
        Widgets.Label(skillLabels, PassionText.Translate());

        // skill weight variation
        var variationSlider = new Rect(textRect.x, textRect.y + RowHeight * 11, textRect.width, RowHeight);
        Editor.SkillWeightVariation = Widgets.HorizontalSlider(variationSlider, Editor.SkillWeightVariation, 1f,
            5.0f, true, SkillVariationText.Translate(Editor.SkillWeightVariation.ToString("P0")),
            SkillVariationLeft.Translate(),
            SkillVariationRight.Translate(), 0.1f);
        TooltipHandler.TipRegion(variationSlider, SkillVariationTooltip.Translate());

        // max passion slider and explainer
        var passionSlider = new Rect(textRect.x, textRect.y + RowHeight * 13, textRect.width, RowHeight);
        Editor.MaxPassionPoints = Widgets.HorizontalSlider(passionSlider, Editor.MaxPassionPoints, 0, 9.0f,
            true, PassionMaxText.Translate(Editor.MaxPassionPoints.ToString("N1")), "0", "9", 0.5f);
        var textExplainer = new Rect(textRect.x, textRect.y + RowHeight * 14, textRect.width, RowHeight * 2);
        var passionPointsNeeded = skillPassions.Sum(x => 1.5f * x.major + 1.0f * x.minor);
        var passionPointsAvailable = Editor.MaxPassionPoints * Find.GameInitData.startingPawnCount;

        TooltipHandler.TipRegion(passionSlider, PassionPointsTooltip.Translate());

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(textExplainer,
            PassionGroupText.Translate($"{passionPointsNeeded:F1}/{passionPointsAvailable:F1}"));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

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

        if (Widgets.ButtonImage(new Rect((float)(rect.width - 24.0 - 6.0), 0.0f, 24f, 24f),
                TexButton.Delete))
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

    private static int GetOverallRowUiLength()
    {
        return int.Parse("Necrofancy.PrepareProcedurally.SelectSkillWidgetLength".Translate());
    }
}
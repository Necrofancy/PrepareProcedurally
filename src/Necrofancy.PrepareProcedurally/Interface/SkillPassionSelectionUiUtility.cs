using System;
using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Necrofancy.PrepareProcedurally.Interface.UiAdjustmentScope;

namespace Necrofancy.PrepareProcedurally.Interface
{
    /// <summary>
    /// UI Utilities to have a drop down of skills and work potential.
    /// </summary>
    /// <remarks>
    /// This is based on the Page_CreateWorldParams and repurposed towards supporting skills for passions in a skill.
    /// </remarks>
    public static class SkillPassionSelectionUiUtility
    {
        private const string SkillSelectWidgetLabel = "SkillPassionSkillSelectWidgetLabel";
        private const string UsableText = "SkillPassionUsable";

        private const string PawnBiology = "SkillPassionPawnBiology";
        private const string AgeRangeText = "SkillPassionAgeRangeLabel";
        private const string MelaninRangeText = "SkillPassionMelaninRangeLabel";
        private const string PassionText = "SkillPassionSkillControls";
        private const string SkillVariationText = "SkillPassionSkillVariationLevel";
        private const string PassionMaxText = "SkillPassionPassionMaxLabel";
        private const string PassionGroupText = "SkillPassionGroupwideLabel";

        private static readonly Lazy<int> SkillTitleLength = new Lazy<int>(GetSkillTitleColumnLength);
        private static readonly Lazy<int> OverallRowLength = new Lazy<int>(GetOverallRowUiLength);

        private const int PlusMinusButtonWidth = 5;
        private const string Plus = "+";
        private const string Minus = "-";

        private static Vector2 scrollPosition;
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
        
        private static IntRange melaninRange = new IntRange(0, PawnSkinColors.SkinColorGenesInOrder.Count - 1);

        public static void DoWindowContents(Rect rect, List<SkillPassionSelection> skillPassions)
        {
            var leftWidth = rect.width - OverallRowLength.Value;
            rect.SplitVertically(leftWidth, out var textRect, out var skillSelectRect);
            
            textRect.y += RowHeight;
            skillSelectRect.y -= 18;
            
            // set up biological page section
            Widgets.Label(new Rect(textRect.x, textRect.y, textRect.width, RowHeight), PawnBiology.Translate());
            var bioRect = new Rect(textRect.x, textRect.y + RowHeight, textRect.width, RowHeight * 5);
            Widgets.DrawMenuSection(bioRect);
            var bioInnerRect = bioRect.GetInnerRect();
            
            // age slider
            var ageSlider = new Rect(bioInnerRect.x, bioInnerRect.y, bioInnerRect.width, RowHeight);
            var ageRange = ProcGen.AgeRange;
            Widgets.IntRange(ageSlider, 1235, ref ageRange, 20, 120, AgeRangeText, minWidth:4);
            ProcGen.AgeRange = ageRange;
            
            // melanin slider
            var melaninSlider = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 1.5f, bioInnerRect.width, RowHeight);
            var genes = PawnSkinColors.SkinColorGenesInOrder;
            var maxMelanin = genes.Count - 1;
            Widgets.IntRange(melaninSlider, 12345, ref melaninRange, 0, maxMelanin, MelaninRangeText, minWidth:1);
            var minSelectedMelanin = genes[melaninRange.min].minMelanin;
            var maxSelectedMelanin = melaninRange.max >= maxMelanin ? 1 : genes[melaninRange.max + 1].minMelanin;
            ProcGen.MelaninRange = new FloatRange(minSelectedMelanin, maxSelectedMelanin);

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
            ProcGen.SkillWeightVariation = Widgets.HorizontalSlider_NewTemp(variationSlider, ProcGen.SkillWeightVariation, 1f, 5.0f, true, SkillVariationText.Translate(ProcGen.SkillWeightVariation.ToString("P0")), "Unvarying", "1-5x variation", 0.1f);
            TooltipHandler.TipRegion(variationSlider, "VariationTooltip".Translate());

            // max passion slider and explainer
            var passionSlider = new Rect(textRect.x, textRect.y + RowHeight * 13, textRect.width, RowHeight);
            ProcGen.MaxPassionPoints = Widgets.HorizontalSlider_NewTemp(passionSlider, ProcGen.MaxPassionPoints, 0, 9.0f, true, PassionMaxText.Translate(ProcGen.MaxPassionPoints.ToString("N1")), "0", "9", 0.5f);
            var textExplainer = new Rect(textRect.x, textRect.y + RowHeight * 14, textRect.width, RowHeight * 2);
            var passionPointsNeeded = skillPassions.Sum(x => 1.5f * x.major + 1.0f * x.minor);
            var passionPointsAvailable = ProcGen.MaxPassionPoints * Find.GameInitData.startingPawnCount;
            
            TooltipHandler.TipRegion(passionSlider, "PassionPointsDescriptionTooltip".Translate());
            
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(textExplainer, PassionGroupText.Translate($"{passionPointsNeeded:F1}/{passionPointsAvailable:F1}"));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            var lineHeight = new Rect(skillSelectRect.x + 20f, skillSelectRect.y, skillSelectRect.width, Text.LineHeight);
            Widgets.Label(lineHeight, SkillSelectWidgetLabel.Translate());
            
            var num1 = Text.LineHeight + 4f;
            var num2 = skillSelectRect.width * 0.050000012f;
            var rect2 = new Rect(skillSelectRect.x + num2, skillSelectRect.y + num1, skillSelectRect.width * 0.9f,
                (float) (skillSelectRect.height - (double) num1 - Text.LineHeight - 28.0));
            var outRect = rect2.ContractedBy(4f);
            var rect3 = new Rect(outRect.x, outRect.y, outRect.width, listingHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect3);
            listingHeight = 0.0f;

            var listingStandard = new Listing_Standard {ColumnWidth = rect3.width};
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

            Widgets.EndScrollView();

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
                    Mathf.Min(rect2.yMax, (float) (outRect.y + (double) listingHeight + 4.0)), outRect.width,
                    AddButtonHeight);
                if (Widgets.ButtonText(rect4, "Add".Translate().CapitalizeFirst() + "..."))
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
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
                    widgetRow.Label(selection.Skill.LabelCap, width: SkillTitleLength.Value);
                }

                // draw major passions selection section
                widgetRow.Gap(GapBetweenKnobs);
                widgetRow.Icon(SkillUI.PassionMajorIcon);
                widgetRow.Gap(-5);

                if (ButtonHit(widgetRow, Minus, selection.major > 0))
                {
                    selection.major--;
                    ProcGen.MakeDirty();
                }

                widgetRow.Gap(GapBeforeNumeric);
                widgetRow.Label(selection.major.ToString(), width: NumericLabelTextLength);
                widgetRow.Gap(GapAfterNumeric);

                if (ButtonHit(widgetRow, Plus, pawnCount > selection.major))
                {
                    if (pawnsRemaining <= 0 && selection.usable > 0)
                        selection.usable--;
                    else if (pawnsRemaining <= 0 && selection.minor > 0)
                        selection.minor--;

                    selection.major++;
                    ProcGen.MakeDirty();
                }

                // draw minor passions selection section if we can give a minor passion role.
                var canAddOrRemovePassions = pawnCount > selection.major;
                if (!canAddOrRemovePassions)
                {
                    GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }

                widgetRow.Gap(GapBetweenKnobs);
                widgetRow.Icon(SkillUI.PassionMinorIcon);
                widgetRow.Gap(-9);

                if (ButtonHit(widgetRow, Minus, selection.minor > 0))
                {
                    selection.minor--;
                    ProcGen.MakeDirty();
                }

                widgetRow.Gap(GapBeforeNumeric);
                widgetRow.Label(selection.minor.ToString(), width: NumericLabelTextLength);
                widgetRow.Gap(GapAfterNumeric);

                if (ButtonHit(widgetRow, Plus, pawnCount > selection.major + selection.minor))
                {
                    if (pawnsRemaining <= 0 && selection.usable > 0)
                        selection.usable--;

                    selection.minor++;
                    ProcGen.MakeDirty();
                }

                // draw minimal non-passion selection section if more pawns can be assigned.
                var canInteractWithMinimalBar = pawnCount > selection.major + selection.minor;
                if (!canInteractWithMinimalBar)
                {
                    GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }

                widgetRow.Gap(GapBetweenKnobs);
                widgetRow.Label(UsableText.Translate());
                widgetRow.Gap(GapAfterNumeric);
                if (ButtonHit(widgetRow, Minus, selection.usable > 0))
                {
                    selection.usable--;
                    ProcGen.MakeDirty();
                }

                widgetRow.Gap(GapBeforeNumeric);
                widgetRow.Label(selection.usable.ToString(), width: NumericLabelTextLength);
                widgetRow.Gap(GapAfterNumeric);

                if (ButtonHit(widgetRow, Plus, pawnsRemaining > 0))
                {
                    selection.usable++;
                    ProcGen.MakeDirty();
                }
            }

            if (Widgets.ButtonImage(new Rect((float) (rect.width - 24.0 - 6.0), 0.0f, 24f, 24f),
                    TexButton.DeleteX))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                factions.RemoveAt(index);
                flag = true;
                ProcGen.MakeDirty();
            }

            Widgets.EndGroup();
            if (Mouse.IsOver(rect1))
            {
                TooltipHandler.TipRegion(rect1, (TipSignal) selection.Skill.description.AsTipTitle());
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
            return int.Parse("SkillPassionSkillTextLength".Translate());
        }

        private static int GetOverallRowUiLength()
        {
            return int.Parse("SkillPassionSkillSelectWidgetOverallLength".Translate());
        }
    }
}
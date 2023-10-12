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
    /// Code was harvested from <see cref="Page_CreateWorldParams"/> and the faction selection, and then
    /// repurposed towards supporting skills for passions in a skill.
    /// </remarks>
    public static class SkillPassionSelectionUiUtility
    {
        private const string SkillSelectWidgetLabel = "SkillPassionSkillSelectWidgetLabel";
        private const string UsableText = "SkillPassionUsable";

        private static readonly Lazy<int> SkillTitleLength = new Lazy<int>(GetSkillTitleColumnLength);
        private static readonly Lazy<int> OverallRowLength = new Lazy<int>(GetOverallRowUiLength);

        private const int PlusMinusButtonWidth = 5;
        private const string Plus = "+";
        private const string Minus = "-";

        private static Vector2 scrollPosition;
        private static float listingHeight;
        private static float warningHeight;

        private const float RowHeight = 24f;
        private const float AddButtonHeight = 28f;

        private const float RowMarginX = 6f;

        // WidgetRow.Button will add a constant value to the fixed width of a button. We have to account for this.
        private const int GapBetweenKnobs = 9;

        private const int NumericLabelTextLength = 10;

        // These are different to even balance 
        private const int GapBeforeNumeric = -3;
        private const int GapAfterNumeric = -2;

        public static bool DoWindowContents(Rect rect, List<SkillPassionSelection> skillPassions, ref IntRange age, ref IntRange melanin)
        {
            bool valuesChanged = false;
            float leftWidth = rect.width - OverallRowLength.Value;
            rect.SplitVertically(leftWidth, out var textRect, out var skillSelectRect);

            var ageSlider = new Rect(textRect.x, textRect.y, textRect.width, RowHeight);
            var melaninSlider = new Rect(textRect.x, textRect.y + RowHeight, textRect.width, RowHeight);

            var max = PawnSkinColors.SkinColorGenesInOrder.Count - 1;

            Widgets.IntRange(ageSlider, 1235, ref age, 15, 120, age.ToString(), minWidth:4);
            Widgets.IntRange(melaninSlider, 12345, ref melanin, 0, max, melanin.ToString(), minWidth:1);

            var lineHeight = new Rect(skillSelectRect.x, skillSelectRect.y, skillSelectRect.width, Text.LineHeight);
            Widgets.Label(lineHeight, SkillSelectWidgetLabel.Translate());
            
            float num1 = Text.LineHeight + 4f;
            float num2 = skillSelectRect.width * 0.050000012f;
            var rect2 = new Rect(skillSelectRect.x + num2, skillSelectRect.y + num1, skillSelectRect.width * 0.9f,
                (float) (skillSelectRect.height - (double) num1 - Text.LineHeight - 28.0) - warningHeight);
            var outRect = rect2.ContractedBy(4f);
            var rect3 = new Rect(outRect.x, outRect.y, outRect.width, listingHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect3);
            listingHeight = 0.0f;

            var listingStandard = new Listing_Standard {ColumnWidth = rect3.width};
            listingStandard.Begin(rect3);
            for (int index = 0; index < skillPassions.Count; ++index)
            {
                listingStandard.Gap(4f);
                if (DoRow(listingStandard.GetRect(RowHeight), skillPassions[index], skillPassions, index,
                        ref valuesChanged))
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
                string str = skillDef.skillLabel;

                void Add()
                {
                    skillPassions.Add(SkillPassionSelection.CreateFromSkill(skillDef));
                    skillPassions.SortByDescending(x => x.Skill.listOrder);
                }

                Func<Rect, bool> defs = (r => Widgets.InfoCardButton(r.x, r.y + 3f, skillDef));
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

            return valuesChanged;
        }

        private static bool DoRow(
            Rect rect,
            SkillPassionSelection selection,
            List<SkillPassionSelection> factions,
            int index,
            ref bool hasChanged)
        {
            int pawnCount = Find.GameInitData.startingPawnCount;
            int pawnsRemaining = pawnCount - selection.major - selection.minor - selection.usable;
            bool flag = false;
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
                    hasChanged = true;
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
                    hasChanged = true;
                }

                // draw minor passions selection section if we can give a minor passion role.
                bool canAddOrRemovePassions = pawnCount > selection.major;
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
                    hasChanged = true;
                }

                widgetRow.Gap(GapBeforeNumeric);
                widgetRow.Label(selection.minor.ToString(), width: NumericLabelTextLength);
                widgetRow.Gap(GapAfterNumeric);

                if (ButtonHit(widgetRow, Plus, pawnCount > selection.major + selection.minor))
                {
                    if (pawnsRemaining <= 0 && selection.usable > 0)
                        selection.usable--;

                    selection.minor++;
                    hasChanged = true;
                }

                // draw minimal non-passion selection section if more pawns can be assigned.
                bool canInteractWithMinimalBar = pawnCount > selection.major + selection.minor;
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
                    hasChanged = true;
                }

                widgetRow.Gap(GapBeforeNumeric);
                widgetRow.Label(selection.usable.ToString(), width: NumericLabelTextLength);
                widgetRow.Gap(GapAfterNumeric);

                if (ButtonHit(widgetRow, Plus, pawnsRemaining > 0))
                {
                    selection.usable++;
                    hasChanged = true;
                }
            }

            if (Widgets.ButtonImage(new Rect((float) (rect.width - 24.0 - 6.0), 0.0f, 24f, 24f),
                    TexButton.DeleteX))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                factions.RemoveAt(index);
                flag = true;
                hasChanged = true;
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
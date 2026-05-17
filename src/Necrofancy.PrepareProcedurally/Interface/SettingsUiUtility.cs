using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Necrofancy.PrepareProcedurally.Solving;
using Necrofancy.PrepareProcedurally.Solving.Weighting;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface;

public class SettingsUiUtility
{
    private const string PawnBiology = "BiologicalSettingsLabel";
    private const string AgeRangeText = "Necrofancy.PrepareProcedurally.BiologicalAgeRange";
    private const string MelaninRangeText = "Necrofancy.PrepareProcedurally.BiologicalMelaninRange";
    private const string InjuriesLabel = "BiologicalAllowInjuriesLabel";
    private const string InjuriesTooltip = "BiologicalAllowInjuriesTooltip";
    private const string RelationshipsLabel = "BiologicalAllowRelationshipsLabel";
    private const string RelationshipsTooltip = "BiologicalAllowRelationshipsTooltip";
    private const string PregnancyLabel = "BiologicalAllowPregnancyLabel";
    private const string PregnancyTooltip = "BiologicalAllowPregnancyTooltip";
    private const string PassionText = "BackstoryPassionLabel";
    private const string SkillVariationText = "Necrofancy.PrepareProcedurally.VariationLabel";
    private const string PassionMaxText = "Necrofancy.PrepareProcedurally.PassionPointsLabel";
    private const string PassionGroupText = "GroupwideUsageIndicator";
    private const string SkillVariationTooltip = "SkillVariationTooltip";
    private const string PassionPointsTooltip = "PassionPointsTooltip";
    private const string SkillVariationLeft = "SkillVariationLeft";
    private const string SkillVariationRight = "SkillVariationRight";
    
    public static float RowHeight { get; } = 24f;
    
    public static int OverallWidth => LazyWidth.Value;

    private static readonly Lazy<int> LazyWidth = new(GetOverallRowUiLength);

    private Action PullChanges { get; }
    private Action PushChanges { get; }

    private ThingDef selectedRace;
    private Dictionary<ThingDef, RaceAgeData> ageRanges;
    private IntRange selectedRaceAgeRange;
    private IntRange allowedAdultRaceAgeRange;
    private IntRange discretizedMelaninRange;
    private FloatRange melaninRange;
    [CanBeNull] private List<SkillPassionSelection> skillPassions;
    private bool allowInjuries;
    private bool allowRelationships;
    private bool allowPregnancy;
    private float skillWeightVariation;
    private float maxPassionPoints;

    public static SettingsUiUtility ForSettings { get; } = new(PrepareMod.Settings);

    public static SettingsUiUtility ForEditor { get; } = new();
    
    private SettingsUiUtility()
    {
        PullChanges = PullFromEditor;
        PushChanges = PropagateToEditor;
        
        selectedRaceAgeRange = Editor.AgeRange;
        melaninRange = Editor.MelaninRange;
        discretizedMelaninRange = GetDiscreteMelaninRange(melaninRange);
        skillPassions = Editor.SkillPassions;
        allowInjuries = Editor.AllowBadHeDiffs;
        allowRelationships = Editor.AllowRelationships;
        allowPregnancy = Editor.AllowPregnancy;
        skillWeightVariation = Editor.SkillWeightVariation;
        maxPassionPoints = Editor.MaxPassionPoints;
    }
    
    private SettingsUiUtility(PrepareModSettings settings)
    {
        void DoNothing() { }
        PullChanges = DoNothing;
        PushChanges = () => PropagateToSettings(settings);
        
        skillPassions = null;
        (selectedRace, ageRanges) = settings.GetLoadedHumanTypes();
        var ageRangeByRace = ageRanges[selectedRace];
        selectedRaceAgeRange = ageRangeByRace.desiredAgeRange;
        allowedAdultRaceAgeRange = ageRangeByRace.possibleAgeRange;
        melaninRange = settings.defaultMelaninRange;
        discretizedMelaninRange = GetDiscreteMelaninRange(melaninRange);
        allowInjuries = settings.allowInjuries;
        allowRelationships = settings.allowRelationships;
        allowPregnancy = settings.allowPregnancy;
        skillWeightVariation = settings.skillWeightVariation;
        maxPassionPoints = settings.maxPassionPoints;
    }
    
    public void DoWindowContents(Rect rect)
    {
        PullChanges();
        
        // set up biological page section
        var labelText = new Rect(rect.x, rect.y, rect.width, RowHeight);
        labelText.SplitVertically(200f, out var labelRect, out var right);
        right = right.ContractedBy(2);
        Widgets.Label(labelRect, PawnBiology.PpTranslate());
        if (ageRanges.Count > 1 && Widgets.ButtonText(right, selectedRace.LabelCap))
        {
            var targets = ageRanges.Keys;

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
        var bioRect = new Rect(rect.x, rect.y + RowHeight, rect.width, RowHeight * bioCount);
        Widgets.DrawMenuSection(bioRect);
        var bioInnerRect = bioRect.GetInnerRect();

        // age slider
        var ageSlider = new Rect(bioInnerRect.x, bioInnerRect.y, bioInnerRect.width, RowHeight);
        
        // Age minimum is to force an adulthood backstory.
        var minAge = allowedAdultRaceAgeRange.min;
        var maxAge = allowedAdultRaceAgeRange.max;

        Widgets.IntRange(ageSlider, 1235, ref selectedRaceAgeRange, minAge, maxAge, AgeRangeText, 4);
        ageRanges[selectedRace] = new RaceAgeData(selectedRaceAgeRange, allowedAdultRaceAgeRange);

        // melanin slider
        var melaninSlider = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 1.5f, bioInnerRect.width, RowHeight);
        var genes = PawnSkinColors.SkinColorGenesInOrder;
        var maxMelanin = genes.Count - 1;
        Widgets.IntRange(melaninSlider, 12345, ref discretizedMelaninRange, 0, maxMelanin, MelaninRangeText, 1);
        var minSelectedMelanin = genes[discretizedMelaninRange.min].minMelanin;
        var maxSelectedMelanin = discretizedMelaninRange.max >= maxMelanin ? 1 : genes[discretizedMelaninRange.max + 1].minMelanin;
        melaninRange = new FloatRange(minSelectedMelanin, maxSelectedMelanin);
        
        // Allow Bad HeDiffs Checkbox
        var allowInjuriesRect = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 4f, bioInnerRect.width, RowHeight);
        Widgets.CheckboxLabeled(allowInjuriesRect, InjuriesLabel.PpTranslate(), ref allowInjuries);
        TooltipHandler.TipRegion(allowInjuriesRect, InjuriesTooltip.PpTranslate());
        
        // Allow Relationships checkbox
        var allowRelationshipsRect = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 5f, bioInnerRect.width, RowHeight);
        Widgets.CheckboxLabeled(allowRelationshipsRect, RelationshipsLabel.PpTranslate(), ref allowRelationships);
        TooltipHandler.TipRegion(allowRelationshipsRect, RelationshipsTooltip.PpTranslate());

        if (ModsConfig.BiotechActive)
        {
            var allowPregnancyRect = new Rect(bioInnerRect.x, bioInnerRect.y + RowHeight * 6f, bioInnerRect.width, RowHeight);
            Widgets.CheckboxLabeled(allowPregnancyRect, PregnancyLabel.PpTranslate(), ref allowPregnancy);
            TooltipHandler.TipRegion(allowPregnancyRect, PregnancyTooltip.PpTranslate());
        }

        var minSkinColor = genes[discretizedMelaninRange.min].IconColor;
        var maxSkinColor = genes[discretizedMelaninRange.max].IconColor;

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
        var skillLabels = new Rect(rect.x, rect.y + RowHeight * 10, rect.width, RowHeight);
        Widgets.Label(skillLabels, PassionText.PpTranslate());

#pragma warning disable CS0612 // 1.4 marks Widgets.HorizontalSlider as obsolete, but keeps it in later versions
        
        // skill weight variation
        var variationSlider = new Rect(rect.x, rect.y + RowHeight * 11, rect.width, RowHeight);
        Text.Font = GameFont.Tiny;
        skillWeightVariation = Widgets.HorizontalSlider(variationSlider, skillWeightVariation, 1f,
            5.0f, true, SkillVariationText.Translate(skillWeightVariation.ToString("P0")),
            SkillVariationLeft.PpTranslate(),
            SkillVariationRight.PpTranslate(), 0.1f);
        Text.Font = GameFont.Small;
        TooltipHandler.TipRegion(variationSlider, SkillVariationTooltip.PpTranslate());

        // max passion slider and explainer
        var passionSlider = new Rect(rect.x, rect.y + RowHeight * 13, rect.width, RowHeight);
        maxPassionPoints = Widgets.HorizontalSlider(passionSlider, maxPassionPoints, 0, 9.0f,
            true, PassionMaxText.Translate(maxPassionPoints.ToString("N1")), "0", "9", 0.5f);
        TooltipHandler.TipRegion(passionSlider, PassionPointsTooltip.PpTranslate());

#pragma warning restore CS0612 // Restore obsolete warning

        PushChanges();

        if (skillPassions is null)
        {
            return;
        }

        var textExplainer = new Rect(rect.x, rect.y + RowHeight * 14, rect.width, RowHeight * 2);
        var passionPointsNeeded = skillPassions.Sum(x => 1.5f * x.major + 1.0f * x.minor);
        var passionPointsAvailable = maxPassionPoints * Find.GameInitData.startingPawnCount;

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(textExplainer,
            PassionGroupText.PpTranslate($"{passionPointsNeeded:F1}/{passionPointsAvailable:F1}"));
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
    }
    
    private void PullFromEditor()
    {
        selectedRace = Editor.SelectedRace;
        ageRanges = Editor.RaceAgeRanges;
        selectedRaceAgeRange = Editor.AgeRange;
        allowedAdultRaceAgeRange = ageRanges[selectedRace].possibleAgeRange;
    }
    
    private static IntRange GetDiscreteMelaninRange(FloatRange melaninRange)
    {
        var range = new IntRange(0, PawnSkinColors.SkinColorGenesInOrder.Count - 1);
        for (var index = 0; index < PawnSkinColors.SkinColorGenesInOrder.Count; index++)
        {
            var gene = PawnSkinColors.SkinColorGenesInOrder[index];
            
            if (Mathf.Approximately(gene.minMelanin, melaninRange.min))
            {
                range.min = index;
            }
            else if (Mathf.Approximately(gene.minMelanin, melaninRange.max))
            {
                range.max = index;
            }
        }

        return range;
    }

    private void PropagateToSettings(PrepareModSettings settings)
    {
        bool dirty = settings.UpdateAgeData(selectedRace, ageRanges);
        void Apply<T>(in T currentValue, ref T value) where T : IEquatable<T>
        {
            if (currentValue.Equals(value))
                return;
            value = currentValue;
            dirty = true;
        }
        
        Apply(melaninRange, ref settings.defaultMelaninRange);
        Apply(allowInjuries, ref settings.allowInjuries);
        Apply(allowRelationships, ref settings.allowRelationships);
        Apply(allowPregnancy, ref settings.allowPregnancy);
        Apply(skillWeightVariation, ref settings.skillWeightVariation);
        Apply(maxPassionPoints, ref settings.maxPassionPoints);

        if (dirty)
        {
            settings.Write();
        }
    }
    
    private void PropagateToEditor()
    {
        Editor.SelectedRace = selectedRace;
        Editor.AgeRange = selectedRaceAgeRange;
        Editor.MelaninRange = melaninRange;
        Editor.AllowBadHeDiffs = allowInjuries;
        Editor.AllowRelationships = allowRelationships;
        Editor.AllowPregnancy = allowPregnancy;
        Editor.SkillWeightVariation = skillWeightVariation;
        Editor.MaxPassionPoints = maxPassionPoints;
    }
    
    private static int GetOverallRowUiLength()
    {
        return int.Parse("SelectSkillWidgetLength".PpTranslate());
    }
}
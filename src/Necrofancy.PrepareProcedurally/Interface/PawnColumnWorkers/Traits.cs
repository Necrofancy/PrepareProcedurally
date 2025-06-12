using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class Traits : PawnColumnWorker
{
    private const string TraitLockedByBackstoryDescription = "Necrofancy.PrepareProcedurally.TraitIsBackstoryTooltip";
    private const string TraitLockedByPlayerChoiceDescription = "Necrofancy.PrepareProcedurally.PlayerRequestedTraitLabel";
    private const string TraitForcedByScenarioOrMod = "Necrofancy.PrepareProcedurally.TraitScenarioForcedLabel";
        
    private static readonly List<Trait> TraitsToRemove = new List<Trait>();
    private static readonly List<TraitRequirement> TraitOptions = new List<TraitRequirement>();
        
    private const int PaddingBetweenButtons = 4;
    const int AddButtonWidth = 30;
        
    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        TraitOptions.Clear();

        var lockedPawn = Editor.LockedPawns.Contains(pawn);
        var needs = lockedPawn ? TraitUtilities.RequiredTraitsForLockedPawn(pawn) : TraitUtilities.RequiredTraitsForUnlockedPawn(pawn);
        TraitOptions.AddRange(TraitUtilities.GetAvailableTraits(needs));
            
        var allowPlusButton = TraitOptions.Any();

        rect = rect.ContractedBy(PaddingBetweenButtons);

        var x = rect.x;
        var y = rect.y;
        float width;
        var height = rect.height;
            
        foreach (var trait in pawn.story.traits.allTraits)
        {
            width = Text.CalcSize(trait.LabelCap).x + 10f;
            var drawRect = new Rect(x, y, width, height);
            DrawTrait(drawRect, trait, pawn);
            x += width + PaddingBetweenButtons;
        }

        if (allowPlusButton && Widgets.ButtonText(new Rect(x, y, AddButtonWidth, height), "+"))
        {
            var options = new List<FloatMenuOption>();
                
            foreach (var option in TraitOptions)
            {
                var str = new Trait(option.def, option.degree ?? 0).LabelCap;
                void Add()
                {
                    if (lockedPawn)
                    {
                        AddTraitToLockedPawn(pawn, option);
                    }
                    else
                    {
                        AddTraitToUnlockedPawn(pawn, option);
                    }
                }

                options.Add(new FloatMenuOption(str, Add));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        foreach (var trait in TraitsToRemove)
        {
            pawn.story.traits.RemoveTrait(trait);
        }
            
        TraitsToRemove.Clear();
    }

    private static void AddTraitToLockedPawn(Pawn pawn, TraitRequirement option)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        Editor.TraitRequirements[index].Add(option);

        List<Trait> geneTraits = new List<Trait>();
        List<Trait> toRemove = new List<Trait>();
        foreach (var ownedTrait in pawn.story.traits.allTraits)
        {
            if (ownedTrait.sourceGene is not null)
            {
                geneTraits.Add(ownedTrait);
                toRemove.Add(ownedTrait);
            }
            else if (option.AllowsTrait(ownedTrait.def))
            {
                toRemove.Add(ownedTrait);
            }
        }

        foreach (var trait in toRemove)
        {
            pawn.story.traits.RemoveTrait(trait);
        }
        
        if (option.def.IsSexualityTrait())
        {
            pawn.story.traits.GainTrait(new Trait(option.def, option.degree ?? 0));
            return;
        }
            
        pawn.story.traits.GainTrait(new Trait(option.def, option.degree ?? 0));
        foreach (var trait in geneTraits)
        {
            pawn.story.traits.GainTrait(trait, suppressConflicts: true);
        }
        
        pawn.story.traits.RecalculateSuppression();
        
        TraitUtilities.FixTraitOverflow(pawn);
    }

    private static void AddTraitToUnlockedPawn(Pawn pawn, TraitRequirement option)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        Editor.TraitRequirements[index].Add(option);
        Editor.MakeDirty();
    }

    public override int GetMinWidth(PawnTable table)
    {
        float maxWidth = 0;
        foreach (var pawn in table.PawnsListForReading)
        {
            var width = pawn.story.traits.allTraits.Sum(trait => Text.CalcSize(trait.LabelCap).x + 10f + 2 * PaddingBetweenButtons);
            if (pawn.story.traits.allTraits.Count < 4)
            {
                width += 2 * PaddingBetweenButtons + AddButtonWidth;
            }

            maxWidth = Math.Max(maxWidth, width);
        }

        return (int)maxWidth;
    }

    private static void DrawTrait(Rect rect, Trait trait, Pawn pawn)
    {
        var color = GUI.color;
        GUI.color = CharacterCardUtility.StackElementBackground;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = color;
        if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
        GUI.color = GetColor(pawn, trait);
        Widgets.Label(new Rect(rect.x + 5f, rect.y, rect.width - 10f, rect.height), trait.LabelCap);
        GUI.color = Color.white;
        if (!Mouse.IsOver(rect)) return;
        var tip = new TipSignal(() => TraitDescriptionWithAdditionalTips(trait, pawn), (int) rect.y * 37);
        TooltipHandler.TipRegion(rect, tip);
        if (Widgets.ButtonInvisible(rect, doMouseoverSound: true))
        {
            var index = StartingPawnUtility.PawnIndex(pawn);
            var requiredTraits = Editor.TraitRequirements[index];
            bool found = false;
            foreach (var required in requiredTraits)
            {
                if (trait.def == required.def && trait.Degree == required.degree)
                {
                    requiredTraits.Remove(required);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                if (CanRemoveTrait(trait, pawn))
                {
                    TraitsToRemove.Add(trait);
                }
            }
        }
    }

    private static bool CanRemoveTrait(Trait trait, Pawn pawn)
    {
        return !TraitUtilities.IsBackstoryTraitOfPawn(trait, pawn) && trait.sourceGene is null && !trait.ScenForced;
    }

    private static string TraitDescriptionWithAdditionalTips(Trait trait, Pawn pawn)
    {
        var builder = new StringBuilder(trait.TipString(pawn));
            
        if (TraitUtilities.IsBackstoryTraitOfPawn(trait, pawn))
        {
            builder.AppendLine().AppendLine().AppendLine(TraitLockedByBackstoryDescription.Translate());
        }

        if (trait.ScenForced)
        {
            builder.AppendLine().AppendLine().AppendLine(TraitForcedByScenarioOrMod.Translate());
        }
            
        var index = StartingPawnUtility.PawnIndex(pawn);
        var forcedTraits = Editor.TraitRequirements[index];
        if (forcedTraits.Any(x => x.def == trait.def && x.degree == trait.Degree))
        {
            builder.AppendLine().AppendLine().AppendLine(TraitLockedByPlayerChoiceDescription.Translate());
        }

        return builder.ToString();
    }

    private static Color GetColor(Pawn pawn, Trait trait)
    {
        if (trait.Suppressed)
        {
            return ColoredText.SubtleGrayColor;
        }

        if (trait.ScenForced)
        {
            return ColorLibrary.Aquamarine;
        }
            
        if (TraitUtilities.IsBackstoryTraitOfPawn(trait, pawn))
        {
            return ColorLibrary.Turquoise;
        }
            
        if (trait.sourceGene != null)
        {
            return ColoredText.GeneColor;
        }

        var index = StartingPawnUtility.PawnIndex(pawn);
        var forcedTraits = Editor.TraitRequirements[index];
        if (forcedTraits.Any(x => x.def == trait.def && x.degree == trait.Degree))
        {
            return ColoredText.ImpactColor;
        }

        return Color.white;
    }
}
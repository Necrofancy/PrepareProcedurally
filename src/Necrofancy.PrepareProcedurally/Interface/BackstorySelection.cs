using System;
using System.Collections.Generic;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface;

public static class BackstorySelection
{
    internal const string ThisBackstoryIsUnlocked = "Necrofancy.PrepareProcedurally.BackstoryUnlocked";
    internal const string ThisBackstoryIsLocked = "Necrofancy.PrepareProcedurally.BackstoryLocked";
    
    public static void PossibleAdulthoods(Pawn pawn, Action onSelect)
    {
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];
        var possibleBackstories = selector.PossibleAdulthoods(traitRequirements);
        
#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawn, possibleBackstories));
#else
        ShowBackstories(possibleBackstories, Editor.SetAdulthoods, Editor.SetChildhoods, index, onSelect);
#endif
    }

    public static void PossibleChildhoods(Pawn pawn, Action onSelect)
    {
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];
        var possibleBackstories = selector.PossibleChildhoods(traitRequirements);
        
#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawn, possibleBackstories));
#else
        ShowBackstories(possibleBackstories, Editor.SetChildhoods, Editor.SetAdulthoods, index, onSelect);
#endif
    }

    public static void ForSelectedPawn(BackstorySlot slot, Rect rect)
    {
        if (!Widgets.ButtonInvisible(rect, doMouseoverSound: true))
        {
            return;
        }
        
        var pawn = SelectedPawn.Pawn;
        var index = SelectedPawn.Index;
        var selector = Compatibility.Layer.GetBackstorySelection(SelectedPawn.Pawn);
        var traitRequirements = Editor.TraitRequirements[index];

        List<BackstoryOption> possibleBackstories = slot switch
        {
            BackstorySlot.Childhood => selector.PossibleChildhoods(traitRequirements),
            BackstorySlot.Adulthood => selector.PossibleAdulthoods(traitRequirements),
            _                       => throw new ArgumentOutOfRangeException(nameof(slot), "Invalid backstory slot")
        };
        
#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawn, possibleBackstories));
#else
        ShowBackstories(possibleBackstories, Editor.SetAdulthoods, Editor.SetChildhoods, index, SelectedPawn.Randomize);
#endif
    }
    
    private static void ShowBackstories(
        IEnumerable<BackstoryOption> backstories, 
        BackstoryDef[] array, 
        BackstoryDef[] otherArray, 
        int pawnIndex,
        Action onSelect)
    {
        var pawn = Editor.StartingPawns[pawnIndex];
        var options = new List<FloatMenuOption>();
        var fixedGender = Editor.GenderRequirements[pawnIndex];
        
        var currentSelection = array[pawnIndex];

        if (currentSelection is not null)
        {
            void Remove()
            {
                if (!currentSelection.shuffleable)
                {
                    // remove both for a shuffleable
                    array[pawnIndex] = null;
                    otherArray[pawnIndex] = null;
                }
                else
                {
                    array[pawnIndex] = null;
                }
                
                onSelect();
            }

            options.Add(new FloatMenuOption("Remove".Translate(), Remove));
        }
        
        foreach (var option in backstories)
        {
            if (fixedGender != GenderPossibility.Either && option.ForcedGender != GenderPossibility.Either
                && option.ForcedGender != fixedGender)
                continue;
            
            var disabled = option.Def == currentSelection;
            
            var floatOption = option.CreateFloatOption(pawn, pawnIndex, disabled, onSelect);

            options.Add(floatOption);
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }
}
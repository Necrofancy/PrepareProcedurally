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
        var childhood = Editor.SetChildhoods[index];
        var possibleBackstories = selector.PossibleAdulthoods(traitRequirements, childhood);
        
#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawn, BackstorySlot.Adulthood, Editor.SetAdulthoods[index], possibleBackstories, onSelect));
#else
        ShowBackstories(possibleBackstories, index, BackstorySlot.Adulthood, onSelect);
#endif
    }

    public static void PossibleChildhoods(Pawn pawn, Action onSelect)
    {
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];
        var adulthood = Editor.SetAdulthoods[index];
        var possibleBackstories = selector.PossibleChildhoods(traitRequirements, adulthood);
        
#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawn, BackstorySlot.Childhood, Editor.SetChildhoods[index], possibleBackstories, onSelect));
#else
        ShowBackstories(possibleBackstories, index, BackstorySlot.Childhood, onSelect);
#endif
    }

    public static void ForSelectedPawn(BackstorySlot slot, Rect rect)
    {
        if (Widgets.ButtonInvisible(rect, doMouseoverSound: true))
        {
            OpenForSelectedPawn(slot);
        }
    }

    public static void OpenForSelectedPawn(BackstorySlot slot)
    {
        var pawn = SelectedPawn.Pawn;
        var index = SelectedPawn.Index;
        var selector = Compatibility.Layer.GetBackstorySelection(SelectedPawn.Pawn);
        var traitRequirements = Editor.TraitRequirements[index];
        
        var childhood = Editor.SetChildhoods[index];
        var adulthood = Editor.SetAdulthoods[index];

        List<BackstoryOption> possibleBackstories;
        BackstoryDef current;

        switch (slot)
        {
            case BackstorySlot.Childhood:
                possibleBackstories = selector.PossibleChildhoods(traitRequirements, adulthood);
                current = childhood;
                break;
            case BackstorySlot.Adulthood:
                possibleBackstories = selector.PossibleAdulthoods(traitRequirements, childhood);
                current = adulthood;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), "Invalid backstory slot");
        }
        
#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawn, slot, current, possibleBackstories, SelectedPawn.Randomize));
#else
        ShowBackstories(possibleBackstories, index, slot, SelectedPawn.Randomize);
#endif
    }
    
    private static void ShowBackstories(
        IEnumerable<BackstoryOption> backstories, 
        int pawnIndex,
        BackstorySlot slot,
        Action onSelect)
    {
        var pawn = Editor.StartingPawns[pawnIndex];
        var options = new List<FloatMenuOption>();
        var fixedGender = Editor.GenderRequirements[pawnIndex];
        
        var currentArray = slot == BackstorySlot.Childhood
            ? Editor.SetChildhoods
            : Editor.SetAdulthoods;
        
        var currentSelection = currentArray[pawnIndex];
        
        if (currentSelection is not null)
        {
            void Remove()
            {
                BackstoryOption.Remove(pawnIndex, slot, onSelect);
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
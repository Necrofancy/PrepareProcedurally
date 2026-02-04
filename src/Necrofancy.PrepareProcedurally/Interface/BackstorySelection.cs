using System;
using System.Collections.Generic;
using System.Text;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Necrofancy.PrepareProcedurally.Solving.StateEdits;
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
        
        ShowBackstories(possibleBackstories, Editor.SetAdulthoods, Editor.SetChildhoods, index, onSelect);
    }

    public static void PossibleChildhoods(Pawn pawn, Action onSelect)
    {
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];
        
        var possibleBackstories = selector.PossibleChildhoods(traitRequirements);
        
        ShowBackstories(possibleBackstories, Editor.SetChildhoods, Editor.SetAdulthoods, index, onSelect);
    }

    public static void ForSelectedPawn(BackstorySlot slot, Rect rect)
    {
        if (!Widgets.ButtonInvisible(rect, doMouseoverSound: true))
        {
            return;
        }
        
        var index = SelectedPawn.Index;
        var selector = Compatibility.Layer.GetBackstorySelection(SelectedPawn.Pawn);
        var traitRequirements = Editor.TraitRequirements[index];

        switch (slot)
        {
            case BackstorySlot.Childhood:
                var possibleBackstories = selector.PossibleChildhoods(traitRequirements);
                ShowBackstories(possibleBackstories, Editor.SetChildhoods, Editor.SetAdulthoods, index, SelectedPawn.Randomize);
                break;
            case BackstorySlot.Adulthood:
                possibleBackstories = selector.PossibleAdulthoods(traitRequirements);
                ShowBackstories(possibleBackstories, Editor.SetAdulthoods, Editor.SetChildhoods, index, SelectedPawn.Randomize);
                break;
            default:
                Logging.ErrorOnce("Unknown backstory slot: " + slot);
                break;
        }
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
            
            var label = CreateLabel(option);
            var disabled = option.Def == currentSelection;
            var tooltip = CreateTooltip(pawn, option);
            void Add()
            {
                if (option.ForcedBio is not null)
                {
                    Editor.SetChildhoods[pawnIndex] = option.ForcedBio.childhood;
                    Editor.SetAdulthoods[pawnIndex] = option.ForcedBio.adulthood;
                }
                else
                {
                    array[pawnIndex] = option.Def;
                    if (otherArray[pawnIndex]?.shuffleable == false)
                    {
                        otherArray[pawnIndex] = null;
                    }
                }
                
                onSelect();
            }
            void ShowTooltip(Rect draw)
            {
                TooltipHandler.TipRegion(draw, tooltip);
            }

            var floatOption = new FloatMenuOption(label, Add, mouseoverGuiAction: ShowTooltip)
            {
                Disabled = disabled
            };

            options.Add(floatOption);
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static string CreateLabel(BackstoryOption option)
    {
        return option.ForcedBio is not null
            ? SolidBioString(option.ForcedBio)
            : option.ForcedGender == GenderPossibility.Female
                ? option.Def.TitleCapFor(Gender.Female)
                : option.Def.TitleCapFor(Gender.Male);
    }

    private static string SolidBioString(PawnBio bio)
    {
        var name = bio.name.ToStringFull;
        return bio.gender switch
        {
            GenderPossibility.Male => $"<color=#06c2ac>{name}</color>",
            GenderPossibility.Female => $"<color=#c79fef>{name}</color>",
            _ => bio.name.ToStringFull
        };
    }

    private static string CreateTooltip(Pawn pawn, BackstoryOption option)
    {
        if (option.ForcedBio is null)
        {
            return option.Def.FullDescriptionFor(pawn);
        }

        using (FakeBackstory(pawn, option.ForcedBio))
        {
            StringBuilder builder = new();
            builder.AppendLine(option.ForcedBio.childhood.FullDescriptionFor(pawn));
            builder.AppendLine();
            builder.AppendLine(option.ForcedBio.adulthood.FullDescriptionFor(pawn));
            return builder.ToString();
        }
    }

    private static TemporaryEdit<(Name, Gender)> FakeBackstory(Pawn pawn, PawnBio bio)
    {
        var current = (pawn.Name, pawn.gender);
        var setGender = bio.gender switch
        {
            GenderPossibility.Male => Gender.Male,
            GenderPossibility.Female => Gender.Female,
            _ => pawn.gender
        };
        var setForBackstoryDesc = (bio.name, setGender);
        void Set((Name name, Gender gender) pawnFacts)
        {
            pawn.Name = pawnFacts.name;
            pawn.gender = pawnFacts.gender;
        }

        return new TemporaryEdit<(Name, Gender)>(current, setForBackstoryDesc, Set);
    }
}
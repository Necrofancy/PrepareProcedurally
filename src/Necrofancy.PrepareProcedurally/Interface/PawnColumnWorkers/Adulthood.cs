using System.Collections.Generic;
using System.Linq;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using RimWorld;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class Adulthood : Backstory
{
    protected override BackstoryDef StoryFrom(Pawn pawn)
    {
        return pawn.story.Adulthood;
    }
    protected override void SelectBackstory(Pawn pawn)
    {
#if !RW1_4 && !RW1_5
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];
        var possibleBackstories = selector.PossibleAdulthoods(traitRequirements);
        
        var storyDefs = possibleBackstories
            .Select(x => x.Def)
            .Where(x => x.shuffleable)
            .ToList();
        
        Find.WindowStack.Add(new AdulthoodSearch(pawn, storyDefs));
#else        
        BackstorySelection.PossibleAdulthoods(pawn, () => OnceSelected(pawn));
#endif
    }

    protected override bool Locked(Pawn pawn)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        return Editor.SetAdulthoods[index] is not null;
    }
}
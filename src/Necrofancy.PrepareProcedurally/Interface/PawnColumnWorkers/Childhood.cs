using System.Linq;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using RimWorld;
using Verse;

// ReSharper disable once UnusedType.Global

namespace Necrofancy.PrepareProcedurally.Interface.PawnColumnWorkers;

public class Childhood : Backstory
{
    protected override BackstoryDef StoryFrom(Pawn pawn)
    {
        return pawn.story.Childhood;
    }
    protected override void SelectBackstory(Pawn pawn)
    {
#if !RW1_4 && !RW1_5
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];
        var possibleBackstories = selector.PossibleChildhoods(traitRequirements);
        
        var storyDefs = possibleBackstories.Select(x => x.Def)
            .Where(x => x.shuffleable)
            .ToList();
        
        Find.WindowStack.Add(new ChildhoodSearch(pawn, storyDefs));
#else
        BackstorySelection.PossibleChildhoods(pawn, () => OnceSelected(pawn));
#endif
    }
    
    protected override bool Locked(Pawn pawn)
    {
        var index = StartingPawnUtility.PawnIndex(pawn);
        return Editor.SetChildhoods[index] is not null;
    }
}
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface.Dialogs;

// an abstraction for allowing some conflation between choosing either a childhood story, an adulthood story,
// or a solid bio that has both as well as a few other factors.
public abstract class BackstoryChoice
{
    // most actions require only differentiating between backstories and bios
    public abstract void Accept(IBackstoryOrBioVisitor visitor);
    
    // some actually need to know whether we are working with a childhood or adulthood backstory
    public abstract void Accept(IBackstoryChoiceVisitor visitor);
}

public class ChildhoodChoice(BackstoryDef backstory) : BackstoryChoice
{
    public override void Accept(IBackstoryChoiceVisitor visitor) => visitor.VisitChildhood(backstory);

    public override void Accept(IBackstoryOrBioVisitor visitor) => visitor.VisitBackstory(backstory);
}

public class AdulthoodChoice(BackstoryDef backstory) : BackstoryChoice
{
    public override void Accept(IBackstoryChoiceVisitor visitor) => visitor.VisitAdulthood(backstory);

    public override void Accept(IBackstoryOrBioVisitor visitor) => visitor.VisitBackstory(backstory);
}

public class SolidBioChoice(PawnBio bio) : BackstoryChoice
{
    public override void Accept(IBackstoryChoiceVisitor visitor) => visitor.VisitSolidBio(bio);

    public override void Accept(IBackstoryOrBioVisitor visitor) => visitor.VisitSolidBio(bio);
}

public interface IBackstoryChoiceVisitor
{
    void VisitChildhood(BackstoryDef childhood);
    void VisitAdulthood(BackstoryDef adulthood);
    void VisitSolidBio(PawnBio bio);
}

public interface IBackstoryOrBioVisitor
{
    void VisitBackstory(BackstoryDef backstory);
    void VisitSolidBio(PawnBio bio);
}

public static class ChoiceFactory
{
    public static IEnumerable<BackstoryChoice> ChildhoodChoices(Pawn pawn)
    {
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];

        // this is a temporary shim for the idea, I might have to have the selector return choices instead.
        foreach (var option in selector.PossibleChildhoods(traitRequirements, Editor.SetAdulthoods[index]))
        {
            yield return option.ForcedBio is not null 
                ? new SolidBioChoice(option.ForcedBio)
                : new ChildhoodChoice(option.Def);
        }
    }
    
    public static IEnumerable<BackstoryChoice> AdulthoodChoices(Pawn pawn)
    {
        var selector = Compatibility.Layer.GetBackstorySelection(pawn);
        var index = StartingPawnUtility.PawnIndex(pawn);
        var traitRequirements = Editor.TraitRequirements[index];

        // this is a temporary shim for the idea, I might have to have the selector return choices instead.
        foreach (var option in selector.PossibleAdulthoods(traitRequirements, Editor.SetChildhoods[index]))
        {
            yield return option.ForcedBio is not null 
                ? new SolidBioChoice(option.ForcedBio)
                : new ChildhoodChoice(option.Def);
        }
    }
}

// label choices will be used for a search menu in 1.6, as well as generating a float menu option for 1.4 and 1.5
public struct LabelChoiceWorker : IBackstoryOrBioVisitor
{
    public LabelChoiceWorker(Pawn pawn, Rect area)
    {
        Pawn = pawn;
        Area = area;
    }

    public Rect Area { get; }
    public Pawn Pawn { get; }
    
    public void VisitBackstory(BackstoryDef backstory)
    {
        var text = backstory.titleFemale is not null
            ? $"{backstory.TitleCapFor(Gender.Male)} / {backstory.TitleCapFor(Gender.Female)}"
            : backstory.TitleCapFor(Gender.Male);

        Widgets.Label(Area, text);
        TooltipHandler.TipRegion(Area, backstory.FullDescriptionFor(Pawn));
    }

    public void VisitSolidBio(PawnBio bio)
    {
        var fullName = BackstorySelection.SolidBioString(bio);
        
        StringBuilder builder = new();
        using (BackstorySelection.FakeBackstory(Pawn, bio))
        {
            builder.AppendLine(bio.childhood.FullDescriptionFor(Pawn));
            builder.AppendLine();
            builder.AppendLine(bio.adulthood.FullDescriptionFor(Pawn));
        }
        
        var tooltip = builder.ToString();
        Widgets.Label(Area, fullName);
        TooltipHandler.TipRegion(Area, tooltip);
    }
}

// applying a choice makes changes to change the editor context to make a pawn randomize again.
public struct ApplyChoiceInBulkEditor : IBackstoryChoiceVisitor
{
    public ApplyChoiceInBulkEditor(Pawn pawn)
    {
        Pawn = pawn;
    }
    
    public Pawn Pawn { get; }
    
    public void VisitChildhood(BackstoryDef childhood)
    {
        var index = StartingPawnUtility.PawnIndex(Pawn);
        Editor.SetChildhoods[index] = childhood;
        Editor.LockedPawns.Remove(Pawn);
        Editor.MakeDirty();
    }

    public void VisitAdulthood(BackstoryDef adulthood)
    {
        var index = StartingPawnUtility.PawnIndex(Pawn);
        Editor.SetAdulthoods[index] = adulthood;
        Editor.LockedPawns.Remove(Pawn);
        Editor.MakeDirty();
    }

    public void VisitSolidBio(PawnBio bio)
    {
        var index = StartingPawnUtility.PawnIndex(Pawn);
        Editor.SetChildhoods[index] = bio.childhood;
        Editor.SetAdulthoods[index] = bio.adulthood;
        Editor.LockedPawns.Remove(Pawn);
        Editor.MakeDirty();
    }
}

public struct ApplyChoiceInSinglePawnEditor : IBackstoryChoiceVisitor
{
    public ApplyChoiceInSinglePawnEditor(Pawn pawn)
    {
        Pawn = pawn;
    }
    
    public Pawn Pawn { get; }
    
    public void VisitChildhood(BackstoryDef childhood)
    {
        var index = StartingPawnUtility.PawnIndex(Pawn);
        Editor.SetChildhoods[index] = childhood;
        SelectedPawn.Randomize();
    }

    public void VisitAdulthood(BackstoryDef adulthood)
    {
        var index = StartingPawnUtility.PawnIndex(Pawn);
        Editor.SetAdulthoods[index] = adulthood;
        SelectedPawn.Randomize();
    }

    public void VisitSolidBio(PawnBio bio)
    {
        var index = StartingPawnUtility.PawnIndex(Pawn);
        Editor.SetChildhoods[index] = bio.childhood;
        Editor.SetAdulthoods[index] = bio.adulthood;
        SelectedPawn.Randomize();
    }
}


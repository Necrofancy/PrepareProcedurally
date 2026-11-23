using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;

namespace Necrofancy.PrepareProcedurally.Interface;

public static class BackstorySelection
{
    internal const string ThisBackstoryIsUnlocked = "Necrofancy.PrepareProcedurally.BackstoryUnlocked";
    internal const string ThisBackstoryIsLocked = "Necrofancy.PrepareProcedurally.BackstoryLocked";

    public static void PossibleAdulthoods(PawnSlot slot, Action onSelect)
    {
        Open(slot, BackstorySlot.Adulthood, onSelect);
    }

    public static void PossibleChildhoods(PawnSlot slot, Action onSelect)
    {
        Open(slot, BackstorySlot.Childhood, onSelect);
    }

    public static void OpenForSelectedPawn(BackstorySlot slot)
    {
        var pawnSlot = SelectedPawn.Slot;
        Open(pawnSlot, slot, SelectedPawn.Randomize);
    }

    public static void Open(PawnSlot pawn, BackstorySlot slot, Action onSelect)
    {
        var choices = PrepareMod.Settings.RuleSet.Choices;
        var (options, current) = slot switch
        {
            BackstorySlot.Childhood => (PossibilitySpace.OfChildhoodOptions(pawn, choices), pawn.ForcedChildhood),
            BackstorySlot.Adulthood => (PossibilitySpace.OfAdulthoodOptions(pawn, choices), pawn.ForcedAdulthood),
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawn, slot, current, options.ToList(), onSelect));
#else
        ShowBackstories(pawn, options, slot, onSelect);
#endif
    }

    public static void ForSelectedPawn(BackstorySlot slot, Rect rect)
    {
        if (Widgets.ButtonInvisible(rect, doMouseoverSound: true))
        {
            OpenForSelectedPawn(slot);
        }
    }

    private static void ShowBackstories(
        PawnSlot pawnSlot,
        IEnumerable<BackstoryOption> backstories,
        BackstorySlot slot,
        Action onSelect)
    {
        var options = new List<FloatMenuOption>();
        var fixedGender = pawnSlot.Gender;

        var currentSelection = slot ==  BackstorySlot.Childhood
            ? pawnSlot.ForcedChildhood
            : pawnSlot.ForcedAdulthood;

        if (currentSelection is not null)
        {
            void Remove()
            {
                pawnSlot.RemoveBackstory(slot);
                onSelect();
            }

            options.Add(new FloatMenuOption("Remove".Translate(), Remove));
        }

        foreach (var option in backstories)
        {
            var disabled = option.Backstory is not null && option.Backstory == currentSelection;

            var floatOption = option.CreateFloatOption(pawnSlot, disabled, onSelect);

            options.Add(floatOption);
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }
}

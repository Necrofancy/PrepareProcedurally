using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface.Dialogs;
using Necrofancy.PrepareProcedurally.Solving.Backgrounds;
using Verse.Sound;

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

    public static void Open(PawnSlot pawnSlot, BackstorySlot slot, Action onSelect)
    {
        var filters = PrepareMod.Settings.RuleSet.RaceRules.GetBackstoryCategoryFilters();
        var selector = new SelectBackstorySpecifically(filters, pawnSlot.Gender, pawnSlot.Pawn);

        var (options, current) = slot switch
        {
            BackstorySlot.Childhood => (selector.PossibleChildhoods(pawnSlot.RequiredTraits, pawnSlot.ForcedAdulthood), pawnSlot.ForcedChildhood),
            BackstorySlot.Adulthood => (selector.PossibleAdulthoods(pawnSlot.RequiredTraits, pawnSlot.ForcedChildhood), pawnSlot.ForcedAdulthood),
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

#if SEARCH_MENU_AVAILABLE
        Find.WindowStack.Add(new BackstorySearch(pawnSlot, slot, current, options, onSelect));
#else
        ShowBackstories(pawnSlot, options, slot, onSelect);
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
            if (fixedGender != GenderPossibility.Either && option.ForcedGender != GenderPossibility.Either
                                                        && option.ForcedGender != fixedGender)
                continue;

            var disabled = option.Def == currentSelection;

            var floatOption = option.CreateFloatOption(pawnSlot, disabled, onSelect);

            options.Add(floatOption);
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }
}

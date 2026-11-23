using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Rules;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public static class PossibilitySpace
{
    public static IEnumerable<BackstoryPossibility> AvoidingAlreadyUsedStories(
        PawnSlot slot,
        IPawnChoices rules,
        IReadOnlyCollection<PawnPossibility> used)
    {
        var defHashes = new HashSet<ushort>();
        foreach (var possibility in used)
        {
            defHashes.Add(possibility.Background.Childhood.shortHash);
            defHashes.Add(possibility.Background.Adulthood.shortHash);
        }

        bool NotAlreadyUsed(BackstoryPossibility possibility)
        {
            return !defHashes.Contains(possibility.Childhood.shortHash)
                && !defHashes.Contains(possibility.Adulthood.shortHash);
        }

        return rules.GetAllPossibilities(slot).Where(NotAlreadyUsed);
    }

    public static IEnumerable<BackstoryPossibility> ChooseDifferentStories(PawnCustomize customization)
    {
        var slot = customization.Slot;
        var options = customization.Rules.Choices;
        Func<BackstoryPossibility, bool> valid = (slot.ForcedChildhood is null, slot.ForcedAdulthood is null) switch
        {
            (true, true) => possibility => !AnyBackstoryMatch(possibility, slot.Pawn),
            (true, false) => possibility => !ChildhoodMatches(possibility, slot.Pawn),
            (false, true) => possibility => !AdulthoodMatches(possibility, slot.Pawn),
            (false, false) => _ => true, // if both are forced there's likely only one option anyways
        };

        return options.GetAllPossibilities(slot).Where(valid);
    }

    public static IEnumerable<BackstoryOption> OfChildhoodOptions(PawnSlot slot, IPawnChoices choices)
    {
        foreach (var childhood in choices.PossibleChildhoodsFor(slot))
            yield return new BackstoryOption(childhood);

        foreach (var bio in choices.PossibleSolidBiosFor(slot))
            yield return new BackstoryOption(bio);
    }

    public static IEnumerable<BackstoryOption> OfAdulthoodOptions(PawnSlot slot, IPawnChoices choices)
    {
        foreach (var childhood in choices.PossibleAdulthoodsFor(slot))
            yield return new BackstoryOption(childhood);

        foreach (var bio in choices.PossibleSolidBiosFor(slot))
            yield return new BackstoryOption(bio);
    }

    private static bool ChildhoodMatches(BackstoryPossibility possibility, Pawn pawn)
    {
        return pawn.story.Childhood == possibility.Childhood;
    }

    private static bool AdulthoodMatches(BackstoryPossibility possibility, Pawn pawn)
    {
        return pawn.story.Adulthood == possibility.Adulthood;
    }

    private static bool AnyBackstoryMatch(BackstoryPossibility possibility, Pawn pawn)
    {
        return ChildhoodMatches(possibility, pawn) || AdulthoodMatches(possibility, pawn);
    }
}

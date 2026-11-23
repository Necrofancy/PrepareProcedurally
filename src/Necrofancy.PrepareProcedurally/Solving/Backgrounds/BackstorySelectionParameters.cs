using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Solving.Weighting;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

/// <summary>
/// Parameters for selecting a pawn backstory.
/// </summary>
/// <param name="Possibilities">An enumeration containing the backstory combination probability space.</param>
/// <param name="WeightSystem">Weighing system for any given possibility.</param>
/// <param name="Selecting">Factors for selecting a possibility from a given pool of good options.</param>
public record BackstorySelectionParameters(
    IEnumerable<BackstoryPossibility> Possibilities,
    WeightBackgroundAlgorithm WeightSystem,
    CandidateSelectionParameters Selecting
)
{
    public int CandidatePoolSize => Selecting.CandidatePoolSize;
    public float MinimumRatioOfBest => Selecting.MinimumRatioOfBest;
    public int? RandomSeed => Selecting.RandomSeed;

    public static BackstorySelectionParameters ForCustomizingPawn(PawnCustomize forPawn)
    {
        return new BackstorySelectionParameters(
            PossibilitySpace.ChooseDifferentStories(forPawn),
            forPawn.Collector.Weight,
            forPawn.CandidateSelectionParameters);
    }

    public static BackstorySelectionParameters ForTeamRebalance(
        TeamRebalance teamRebalance,
        PawnSlot slot,
        WeightBackgroundAlgorithm weightSystem,
        List<PawnPossibility> doneSoFar)
    {
        return new BackstorySelectionParameters(
            PossibilitySpace.AvoidingAlreadyUsedStories(slot, teamRebalance.RuleSet.Choices, doneSoFar),
            weightSystem,
            teamRebalance.StorySelection
        );
    }
}

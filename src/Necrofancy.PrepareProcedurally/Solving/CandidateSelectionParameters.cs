namespace Necrofancy.PrepareProcedurally.Solving;

public record CandidateSelectionParameters(int CandidatePoolSize, float MinimumRatioOfBest, int? RandomSeed = null)
{
    public static CandidateSelectionParameters FromSettings()
    {
        var candidatePoolSize = PrepareMod.Settings.candidatePoolSize;
        var minimumRatioOfBest = PrepareMod.Settings.minimumRatioOfBest;
        return new CandidateSelectionParameters(candidatePoolSize, minimumRatioOfBest);
    }
}

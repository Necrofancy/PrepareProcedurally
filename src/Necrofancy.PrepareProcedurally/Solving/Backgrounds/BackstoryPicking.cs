namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public static class BackstoryPicking
{
    public static BackstoryPossibility TopWeighted(BackstorySelectionParameters selectionParameters)
    {
        if (!selectionParameters.RandomSeed.HasValue)
            return TopWeightedInternal(selectionParameters);

        try
        {
            Rand.PushState(selectionParameters.RandomSeed.Value);
            return TopWeightedInternal(selectionParameters);
        }
        finally
        {
            Rand.PopState();
        }
    }

    private static BackstoryPossibility TopWeightedInternal(BackstorySelectionParameters selectionParameters)
    {
        var top = new List<(BackstoryPossibility Bio, float Ranking)>(selectionParameters.CandidatePoolSize + 1);
        foreach (var bio in selectionParameters.Possibilities)
        {
            var ranking = selectionParameters.WeightSystem(bio);

            var candidate = (bio, ranking);
            if (top.Count == selectionParameters.CandidatePoolSize && top[top.Count - 1].Ranking > ranking)
                continue;

            var insertAt = top.Count;
            while (insertAt > 0
                   && (ranking > top[insertAt - 1].Ranking
                       || (Mathf.Approximately(ranking, top[insertAt - 1].Ranking) && Rand.Bool)))
                insertAt--;
            top.Insert(insertAt, candidate);
            if (top.Count > selectionParameters.CandidatePoolSize)
                top.RemoveAt(top.Count - 1);
        }

        if (top.Count == 0)
            throw new InvalidOperationException("No valid pawn possibilities were found.");

        var bestRanking = top[0].Ranking;
        var goodOptionsExist = bestRanking > 0;
        var floor = goodOptionsExist ? bestRanking * selectionParameters.MinimumRatioOfBest : bestRanking;
        var eligible = top.Where(x => x.Ranking >= floor).ToList();

        var picked = goodOptionsExist
            ? eligible.RandomElementByWeight(x => x.Ranking).Bio
            : eligible.RandomElement().Bio;
        return picked;
    }
}

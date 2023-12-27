using System.Collections.Generic;
using RimWorld;

namespace Necrofancy.PrepareProcedurally.Solving.Backgrounds;

public static class PossibleBackstories
{
    private static readonly Dictionary<string, List<PawnBio>> Cache = new Dictionary<string, List<PawnBio>>();

    public static List<PawnBio> FromCategoryName(string category)
    {
        if (!Cache.TryGetValue(category, out var existing))
        {
            existing = new List<PawnBio>();
            foreach (var bio in SolidBioDatabase.allBios)
            {
                if (bio.adulthood.spawnCategories.Contains(category))
                {
                    existing.Add(bio);
                }
            }

            Cache[category] = existing;
        }

        return existing;
    }
}
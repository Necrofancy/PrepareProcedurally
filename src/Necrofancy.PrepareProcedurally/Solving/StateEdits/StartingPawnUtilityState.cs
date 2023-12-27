using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving;

internal static class StartingPawnUtilityState
{
    public static List<PawnGenerationRequest> GetStartingPawnRequestList()
    {
        var type = typeof(StartingPawnUtility);
        var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var field in fields)
        {
            if (field.Name.Equals("StartingAndOptionalPawnGenerationRequests")
                && field.GetValue(null) is List<PawnGenerationRequest> requests)
            {
                return requests;
            }
        }

        return null;
    }
}
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace Necrofancy.PrepareProcedurally.Solving
{
    internal static class StartingPawnUtilityState
    {
        private static readonly List<PawnGenerationRequest> RequestsCached;

        static StartingPawnUtilityState()
        {
            var type = typeof(StartingPawnUtility);
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.Name.Equals("StartingAndOptionalPawnGenerationRequests")
                    && field.GetValue(null) is List<PawnGenerationRequest> requests)
                {
                    RequestsCached = requests;
                }
            }
        }

        public static List<PawnGenerationRequest> Requests => RequestsCached;
    }
}
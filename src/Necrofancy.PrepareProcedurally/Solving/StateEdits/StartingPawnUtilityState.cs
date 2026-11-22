using System.Reflection;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

internal static class StartingPawnUtilityState
{
    private static readonly FieldInfo startingAndOptionalPawns;
    private static readonly MethodInfo ensureGenerationRequestInRangeOf;

    static StartingPawnUtilityState()
    {
        var type = typeof(StartingPawnUtility);
        var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var field in fields)
        {
            if (field.Name.Equals("StartingAndOptionalPawnGenerationRequests"))
            {
                startingAndOptionalPawns = field;
            }
        }

        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var method in methods)
        {
            if (method.Name.Equals("EnsureGenerationRequestInRangeOf"))
            {
                ensureGenerationRequestInRangeOf = method;
            }
        }
    }

    public static List<PawnGenerationRequest> GetStartingPawnRequestList()
    {
        ensureGenerationRequestInRangeOf.Invoke(null, [EditorSession.Require().Slots.Count]);
        return (List<PawnGenerationRequest>)startingAndOptionalPawns.GetValue(null);
    }

    public static void SetStartingPawnRequestList(List<PawnGenerationRequest> requests)
    {
        startingAndOptionalPawns.SetValue(null, requests);
    }
}

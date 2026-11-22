using JetBrains.Annotations;
using Necrofancy.PrepareProcedurally.Editing;

namespace Necrofancy.PrepareProcedurally.Interface;

public static class EditorSession
{
    [CanBeNull] public static EditorContext Current { get; private set; }

    public static EditorContext Require()
    {
        return Current ?? throw new InvalidOperationException("Editor session not started yet");
    }

    internal static void Begin()
    {
        if (Current is null)
        {
            Current = EditorContext.LiveContext();
            Debug.Log("Initializing editor session");
        }
    }

    internal static void End()
    {
        Current = null;
        Debug.Log("Ending editor session");
    }
}

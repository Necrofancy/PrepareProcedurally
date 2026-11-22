using System.Diagnostics;
using System.Runtime.CompilerServices;
using Necrofancy.PrepareProcedurally.Editing;
using Necrofancy.PrepareProcedurally.Interface;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public class Logging
{
    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(string message)
    {
        Log.Message($"[PrepareProcedurally DEBUG] {message}");
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Error(message);
#if DEBUG
            EditorSession.Current?.StressTestMode = false;
#endif
        }
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertDefMatch<TDef>(TDef expected, TDef actual) where TDef : Def
    {
        Assert(expected.Equals(actual), $"expected def '{expected.defName}' but got '{actual.defName}'");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(string message)
    {
        Log.Message($"[PrepareProcedurally] {message}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(string message)
    {
        Log.Warning($"[PrepareProcedurally WARN] {message}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(string message)
    {
        Log.Error($"[PrepareProcedurally ERROR] {message}");
    }

    public static void ErrorOnce(string message, [CallerFilePath] string caller = "", [CallerLineNumber] int line = 0)
    {
        int hashcode = caller.GetHashCode() ^ line;
        Log.ErrorOnce($"[PrepareProcedurally] {message}", hashcode);
    }


}

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Verse;

namespace Necrofancy.PrepareProcedurally;

public class Logging
{
    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
        Log.Message($"[PrepareProcedurally DEBUG] {message}");
    }
    
    public static void Info(string message)
    {
        Log.Message($"[PrepareProcedurally] {message}");
    }
    
    public static void Warn(string message)
    {
        Log.Warning($"[PrepareProcedurally] {message}");
    }
    
    public static void Error(string message)
    {
        Log.Error($"[PrepareProcedurally] {message}");
    }

    public static void ErrorOnce(string message, [CallerFilePath]string caller = "", [CallerLineNumber] int line = 0)
    {
        int hashcode = caller.GetHashCode() ^ line;
        Log.ErrorOnce($"[PrepareProcedurally] {message}", hashcode);
    }
}
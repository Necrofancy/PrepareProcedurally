using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    public static void ErrorOnce(string message, [CallerFilePath]string caller = "", [CallerLineNumber] int line = 0)
    {
        int hashcode = caller.GetHashCode() ^ line;
        Log.ErrorOnce($"[PrepareProcedurally] {message}", hashcode);
    }
}
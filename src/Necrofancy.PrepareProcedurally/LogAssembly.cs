using System.Reflection;
using JetBrains.Annotations;
using Verse;

namespace Necrofancy.PrepareProcedurally
{
    [StaticConstructorOnStartup, UsedImplicitly]
    public static class LogAssembly
    {
        static LogAssembly()
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            Log.Message($"{assembly.Name}: v{assembly.Version}");
        }
    }
}
using Verse;

namespace Necrofancy.PrepareProcedurally;

public static class TranslationExtensions
{
    public static TaggedString PpTranslate(this string key) => $"Necrofancy.PrepareProcedurally.{key}".Translate();
    
#if RW1_6
    public static TaggedString PpTranslate(this string key, params NamedArgument[] args) => $"Necrofancy.PrepareProcedurally.{key}".Translate(args);
#else
    public static TaggedString PpTranslate(this string key, params object[] args) => $"Necrofancy.PrepareProcedurally.{key}".Translate(args);
#endif
}
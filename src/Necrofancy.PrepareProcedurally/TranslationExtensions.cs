using Verse;

namespace Necrofancy.PrepareProcedurally;

public static class TranslationExtensions
{
    public static TaggedString PpTranslate(this string key) => $"Necrofancy.PrepareProcedurally.{key}".Translate();
    public static TaggedString PpTranslate(this string key, params NamedArgument[] args) => $"Necrofancy.PrepareProcedurally.{key}".Translate(args);

}
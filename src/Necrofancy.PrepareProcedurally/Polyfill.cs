#if !WAYLITH

using JetBrains.Annotations;

// ReSharper disable once CheckNamespace

// "The predefined type 'System.Runtime.CompilerServices.IsExternalInit' must be defined or imported
// in order to declare init-only setter"

// Thy wish has been granted, compiler.
namespace System.Runtime.CompilerServices
{
    [UsedImplicitly]
    internal static class IsExternalInit
    {
    }
}
#endif
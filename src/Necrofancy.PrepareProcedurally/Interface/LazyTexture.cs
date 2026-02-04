using System;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface;


public static class LazyTexture
{
    public static Lazy<Texture2D> Unlocked { get; } = "UI/Overlays/LockedMonochrome".AsTexture();
    public static Lazy<Texture2D> Locked { get; } = "UI/Overlays/Locked".AsTexture();
    
    /// <summary>
    /// I want to reference a texture before it's potentially loaded by the game.
    /// </summary>
    public static Lazy<Texture2D> AsTexture(this string resource)
    {
        return new Lazy<Texture2D>(() => ContentFinder<Texture2D>.Get(resource));
    }
    
    /// <summary>
    /// Lazy load any related textures to avoid having something try resolving off the UI thread at start.
    /// </summary>
    private static Lazy<Texture2D> LazyLoad(string constString)
    {
        return new Lazy<Texture2D>(() => ContentFinder<Texture2D>.Get(constString));
    }
}
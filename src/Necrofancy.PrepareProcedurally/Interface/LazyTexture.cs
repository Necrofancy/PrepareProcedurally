using System;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface;

public static class HelperExtensions
{
    /// <summary>
    /// I want to reference a texture before it's potentially loaded by the game.
    /// </summary>
    public static Lazy<Texture2D> AsTexture(this string resource)
    {
        return new Lazy<Texture2D>(() => ContentFinder<Texture2D>.Get(resource));
    }
}
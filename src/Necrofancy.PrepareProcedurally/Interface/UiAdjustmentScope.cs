using System;
using UnityEngine;
using Verse;

namespace Necrofancy.PrepareProcedurally.Interface
{
    public static class UiAdjustmentScope
    {
        public static IDisposable TextAnchorOf(TextAnchor anchor) => new TemporaryAnchor(anchor);
        public static IDisposable BackgroundColorOf(Color color) => new TemporaryBackgroundColor(color);
        public static IDisposable ForegroundColorOf(Color color) => new TemporaryColor(color);
        public static IDisposable TextWrapOf(bool doWrap) => new TemporaryWordWrap(doWrap);
        
        private readonly struct TemporaryAnchor : IDisposable
        {
            private readonly TextAnchor previous;

            public TemporaryAnchor(TextAnchor anchor)
            {
                previous = Text.Anchor;
                Text.Anchor = anchor;
            }

            void IDisposable.Dispose() => Text.Anchor = previous;
        }
        
        private readonly struct TemporaryBackgroundColor : IDisposable
        {
            private readonly Color previous;

            public TemporaryBackgroundColor(Color color)
            {
                previous = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            void IDisposable.Dispose() => GUI.backgroundColor = previous;
        }
    
        private readonly struct TemporaryColor : IDisposable
        {
            private readonly Color previous;

            public TemporaryColor(Color color)
            {
                previous = GUI.color;
                GUI.color = color;
            }

            void IDisposable.Dispose() => GUI.color = previous;
        }

        private readonly struct TemporaryWordWrap : IDisposable
        {
            private readonly bool previous;
            
            public TemporaryWordWrap(bool doWrap)
            {
                previous = Text.WordWrap;
                Text.WordWrap = doWrap;
            }

            void IDisposable.Dispose() => Text.WordWrap = previous;
        }
    }
}
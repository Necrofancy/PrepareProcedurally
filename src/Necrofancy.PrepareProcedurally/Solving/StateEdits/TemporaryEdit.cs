using System;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits
{
    /// <summary>
    /// Make a temporary edit of some state
    /// </summary>
    public struct TemporaryEdit<T> : IDisposable
    {
        private readonly T _oldValue;
        private readonly Action<T> _setter;

        public TemporaryEdit(T oldValue, T newValue, Action<T> setter)
        {
            _oldValue = oldValue;
            _setter = setter;

            _setter(newValue);
        }

        public void Dispose() => _setter(_oldValue);
    }
}
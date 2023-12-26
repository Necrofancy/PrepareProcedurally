using System;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits
{
    /// <summary>
    /// Make a temporary edit of some state
    /// </summary>
    public readonly struct TemporaryEdit<T> : IDisposable
    {
        private readonly T oldValue;
        private readonly Action<T> setter;

        public TemporaryEdit(T oldValue, T newValue, Action<T> setter)
        {
            this.oldValue = oldValue;
            this.setter = setter;

            this.setter(newValue);
        }

        public void Dispose() => setter(oldValue);
    }

    public static class TemporaryEdit
    {
        public static IDisposable NullEdit => new TemporaryEdit<int>(default, default, DoNothingWithData);

        private static void DoNothingWithData(int _)
        {
            
        }
    }
}
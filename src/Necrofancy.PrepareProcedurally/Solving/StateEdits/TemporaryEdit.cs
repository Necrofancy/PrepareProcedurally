using System;
using System.Collections.Generic;
using System.Linq;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

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

    public static IDisposable Many<T>(Stack<T> disposables) where T:IDisposable => new TemporaryMultiple<T>(disposables);

    private static void DoNothingWithData(int _)
    {
            
    }

    private class TemporaryMultiple<T> : IDisposable where T : IDisposable
    {
        private readonly Stack<T> multiple;

        public TemporaryMultiple(Stack<T> multiple)
        {
            this.multiple = multiple;
        }

        public void Dispose()
        {
            while (multiple.Any())
            {
                var disposable = multiple.Pop();
                disposable.Dispose();
            }
        }
    }
}
using HarmonyLib;

namespace Necrofancy.PrepareProcedurally.Solving.StateEdits;

public sealed class Hack
{
    private readonly Action doHack;
    private readonly Action undoHack;

    private bool done;

    private Hack(Action doHack, Action undoHack, string description )
    {
        Description = description;
        this.doHack = doHack;
        this.undoHack = undoHack;
    }

    public string Description { get; }

    internal void Do()
    {
        if (done)
        {
            Logging.ErrorOnce("Do on a hack-edit called twice; this shouldn't happen.");
            return;
        }

        doHack();
        done = true;
    }

    internal void Undo()
    {
        if (!done)
        {
            Logging.ErrorOnce("Undo on a hack-edit called twice; this shouldn't happen.");
            return;
        }

        undoHack();
        done = false;
    }

    public static Hack Function<TValue>(Func<TValue> get, Func<TValue, TValue> transform, Action<TValue> set, string description)
    {
        var oldValue = default(TValue);

        void Do()
        {
            oldValue = get();
            set(transform(oldValue));
        }

        void Undo()
        {
            set(oldValue);
        }

        return new Hack(Do, Undo, description);
    }

    public static Hack Value<TValue>(Func<TValue> get, TValue value, Action<TValue> set, string description)
        => Function(get, _ => value, set, description);

    public static Hack PropertyTransform<TObject, TProperty>(TObject instance, string propName, Func<TProperty,TProperty> transform)
        where TObject : class
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));
        if (propName is null)
            throw new ArgumentNullException(nameof(propName));

        var propertyName = $"{instance.GetType().Name}.{propName}";
        var getter = AccessTools.PropertyGetter(instance.GetType(), propName);
        var setter = AccessTools.PropertySetter(instance.GetType(), propName);

        if (getter is null || setter is null)
            throw new InvalidOperationException($"{propertyName} getter/setter not found on {instance.GetType().Name}.");
        if (getter.ReturnType != typeof(TProperty))
            throw new InvalidOperationException($"{propertyName} type mismatch on type {typeof(TProperty).Name}.");

        TProperty Get() => (TProperty)getter.Invoke(instance,[]);
        void Set(TProperty value) => setter.Invoke(instance, [value]);
        return Function(Get, transform, Set, propertyName);
    }

    public static Hack PropertySet<TObject, TProp>(TObject instance, string propName, TProp value)
        where TObject : class => PropertyTransform(instance, propName, (TProp _) => value);

    public static Hack FieldTransform<TObject, TField>(TObject instance, string fieldName, Func<TField, TField> transform)
        where TObject : class
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));
        if (fieldName is null)
            throw new ArgumentNullException(nameof(fieldName));

        var fieldFullName = $"{instance.GetType().Name}.{fieldName}";
        var fieldInfo = AccessTools.Field(instance.GetType(), fieldName);

        if (fieldInfo is null)
            throw new InvalidOperationException($"{fieldFullName} not found on {instance.GetType().Name}.");
        if (fieldInfo.FieldType != typeof(TField))
            throw new InvalidOperationException($"{fieldFullName} mismatch on type {typeof(TField).Name}.");

        TField Get() => (TField)fieldInfo.GetValue(instance);
        void Set(TField value) => fieldInfo.SetValue(instance, value);
        return Function(Get, transform, Set, fieldFullName);
    }

    public static Hack FieldWithValue<TObject, TField>(TObject instance, string fieldName, TField value)
        where TObject : class => FieldTransform(instance, fieldName, (TField _) => value);
}

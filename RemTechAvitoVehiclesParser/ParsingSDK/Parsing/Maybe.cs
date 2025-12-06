namespace ParsingSDK.Parsing;

public sealed class Maybe<T> where T : notnull
{
    public bool HasValue { get; }
    public T Value => HasValue
        ? field
        : throw new InvalidOperationException($"Cannot access none value of {nameof(Maybe<>)}");

    private Maybe(T value)
    {
        Value = value;
        HasValue = true;
    }

    private Maybe()
    {
        Value = default!;
        HasValue = false;
    }
    
    public static Maybe<T> Some(T value) => new(value);
    public static Maybe<T> None() => new();
    public static Maybe<T> Resolve(T? value) => value == null ? None() : Some(value);
    
    public static async Task<Maybe<T>> Resolve(Func<Task<T?>> fn)
    {
        T? value = await fn();
        return Resolve(value);
    }
}
#pragma warning disable SA1649

namespace MapTo.Mappings.Handlers;

internal enum HandlerResultKind
{
    Undetermined,
    Success,
    Failure
}

internal readonly record struct ResolverResult<T>(T Value, Diagnostic? Error, HandlerResultKind Kind)
{
    public bool IsSuccess => Kind == HandlerResultKind.Success;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public bool IsUndetermined => Kind == HandlerResultKind.Undetermined;

    public static implicit operator ResolverResult<T>(T value) => new(value, null, HandlerResultKind.Success);

    public static implicit operator ResolverResult<T>(Diagnostic error) => new(default!, error, HandlerResultKind.Failure);
}

internal static class ResolverResult
{
    public static ResolverResult<T> Failure<T>(Diagnostic? error) => new(default!, error, HandlerResultKind.Failure);

    public static ResolverResult<T> Success<T>(T value) => new(value, null, HandlerResultKind.Success);

    public static ResolverResult<T> Success<T>() => new(default!, null, HandlerResultKind.Success);

    public static ResolverResult<T> Undetermined<T>(Diagnostic? error = null) => new(default!, error, HandlerResultKind.Undetermined);
}
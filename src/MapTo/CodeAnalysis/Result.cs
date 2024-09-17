namespace MapTo.CodeAnalysis;

internal readonly record struct Result<T>(T Value, Diagnostic? Error, ResultKind Kind)
{
    public bool IsSuccess => Kind == ResultKind.Success;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public bool IsUndetermined => Kind == ResultKind.Undetermined;

    public static implicit operator Result<T>(T value) => new(value, null, ResultKind.Success);

    public static implicit operator Result<T>(Diagnostic error) => new(default!, error, ResultKind.Failure);
}

internal static class Result
{
    public static Result<T> Failure<T>(Diagnostic error) => new(default!, error, ResultKind.Failure);

    public static Result<T> Success<T>(T value) => new(value, null, ResultKind.Success);

    public static Result<T> Undetermined<T>(Diagnostic? error = null) => new(default!, error, ResultKind.Undetermined);
}

internal enum ResultKind
{
    Undetermined,
    Success,
    Failure
}
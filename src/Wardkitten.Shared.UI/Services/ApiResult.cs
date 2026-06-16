namespace Wardkitten.Shared.UI.Services;

public sealed record ApiResult(bool Ok, string? Error)
{
    public static ApiResult Success() => new(true, null);
    public static ApiResult Failure(string error) => new(false, error);
}

public sealed record ApiResult<T>(bool Ok, T? Value, string? Error)
{
    public static ApiResult<T> Success(T value) => new(true, value, null);
    public static ApiResult<T> Failure(string error) => new(false, default, error);
}

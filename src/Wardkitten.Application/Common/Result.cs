namespace Wardkitten.Application.Common;

/// <summary>Resultado de una operación de servicio sin valor de retorno.</summary>
public readonly record struct Result(bool Success, string? Error)
{
    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

/// <summary>Resultado de una operación de servicio con valor.</summary>
public readonly record struct Result<T>(bool Success, T? Value, string? Error)
{
    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}

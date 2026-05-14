using System.Diagnostics.CodeAnalysis;

namespace YenkoTools.Common.Cqrs.Results;

public class Result
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }
    public Error Error { get; }

    public bool IsNotFound => !IsSuccess && (Error.Code.StartsWith("NotFound") || Error == Error.NotFound);

    protected Result(bool isSuccess, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Error = Error.None;
    }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
            throw new ArgumentException("Invalid error", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
        ErrorMessage = error.ToString();
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string errorMessage) => new(false, errorMessage);
    public static Result Failure(Error error) => new(false, error);

    public static Result NotFound(Error error) => new(false, error);
    public static Result NotFound(string resourceName = "resource") =>
        new(false, new Error($"NotFound.{resourceName}", $"The requested {resourceName} was not found"));

    public override string ToString() => IsSuccess ? "Success" : $"Failure: {ErrorMessage}";
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can't be accessed.");

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure(Error.NullValue);

    public static Result<TValue> Success(TValue value) => new(value, true, Error.None);
    public static new Result<TValue> Failure(Error error) => new(default, false, error);

    public new static Result<TValue> NotFound(Error error) => new(default, false, error);
    public new static Result<TValue> NotFound(string resourceName = "") =>
        new(default, false, new Error(
            $"NotFound.{resourceName ?? typeof(TValue).Name}",
            $"The requested {resourceName ?? typeof(TValue).Name} was not found"));
}

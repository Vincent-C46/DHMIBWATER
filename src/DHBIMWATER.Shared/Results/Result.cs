namespace DHBIMWATER.Shared.Results;

/// <summary>
/// 작업 결과를 나타내는 래퍼 클래스
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("성공 결과는 에러를 가질 수 없습니다.");

        if (!isSuccess && error == null)
            throw new InvalidOperationException("실패 결과는 에러가 필요합니다.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);

    public static Result<T> Failure<T>(Error error) => new(default!, false, error);
}

/// <summary>
/// 값을 포함하는 작업 결과
/// </summary>
public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(T value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        Value = value;
    }
}

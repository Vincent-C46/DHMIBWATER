namespace DHBIMWATER.Shared.Results;

/// <summary>
/// 에러 정보
/// </summary>
public sealed class Error : IEquatable<Error>
{
    public string Code { get; }
    public string Message { get; }

    public Error(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public static Error None => new(string.Empty, string.Empty);

    public static Error NullValue => new("Error.NullValue", "값이 null입니다.");

    public bool Equals(Error? other)
    {
        if (other is null) return false;
        return Code == other.Code && Message == other.Message;
    }

    public override bool Equals(object? obj) => obj is Error other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Code, Message);

    public override string ToString() => $"[{Code}] {Message}";
}

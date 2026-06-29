namespace UserService.Application.Common.Exceptions;

/// <summary>
/// Thrown for expected, client-facing error conditions (validation, auth failures, not found).
/// The API layer maps this to an appropriate HTTP status code.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

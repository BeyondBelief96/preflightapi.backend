namespace PreflightApi.Domain.Exceptions;

/// <summary>
/// Base exception for resource conflicts (duplicate entries, resources in use, etc.).
/// </summary>
public class ConflictException : DomainException
{
    public ConflictException(string errorCode, string userMessage, string? devMessage = null)
        : base(errorCode, userMessage, devMessage)
    {
    }
}

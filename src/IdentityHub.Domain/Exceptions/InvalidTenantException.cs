namespace IdentityHub.Domain.Exceptions;

/// <summary>
/// Exception thrown when tenant context is invalid or missing
/// </summary>
public class InvalidTenantException : Exception
{
    public InvalidTenantException()
        : base("Valid tenant context is required for this operation")
    {
    }

    public InvalidTenantException(string message)
        : base(message)
    {
    }

    public InvalidTenantException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

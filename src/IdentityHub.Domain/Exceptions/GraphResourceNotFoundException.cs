namespace IdentityHub.Domain.Exceptions;

/// <summary>
/// Exception thrown when a resource is not found in Microsoft Graph API
/// </summary>
public class GraphResourceNotFoundException : Exception
{
    public GraphResourceNotFoundException()
        : base("The requested resource was not found in Microsoft Graph")
    {
    }

    public GraphResourceNotFoundException(string message)
        : base(message)
    {
    }

    public GraphResourceNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates exception with resource type and identifier
    /// </summary>
    public static GraphResourceNotFoundException ForUser(string userId)
        => new GraphResourceNotFoundException($"User '{userId}' was not found in Microsoft Graph");

    /// <summary>
    /// Creates exception with resource type and identifier
    /// </summary>
    public static GraphResourceNotFoundException ForGroup(string groupId)
        => new GraphResourceNotFoundException($"Group '{groupId}' was not found in Microsoft Graph");
}

using System.Net;
using System.Text.Json;
using IdentityHub.Domain.Exceptions;

namespace IdentityHub.API.Middleware;

/// <summary>
/// Global exception handler middleware
/// Catches unhandled exceptions and returns appropriate error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, message) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.InternalServerError,
                "Service configuration error. Please contact support."),

            InvalidTenantException => (HttpStatusCode.BadRequest,
                "Invalid or missing tenant context. Please ensure tenant information is provided."),

            UnauthorizedAccessException => (HttpStatusCode.Forbidden,
                "You don't have permission to access this resource."),

            ArgumentNullException or ArgumentException => (HttpStatusCode.BadRequest,
                "Invalid request parameters."),

            KeyNotFoundException => (HttpStatusCode.NotFound,
                "The requested resource was not found."),

            _ => (HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later.")
        };

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            TraceId = context.TraceIdentifier
        };

        // Include details in development
        if (_environment.IsDevelopment())
        {
            response.Details = exception.Message;
            response.StackTrace = exception.StackTrace;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Error response model
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}

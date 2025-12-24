using NeuroPath.Models.DTOs;
using System.Text.Json;
using System.Net;

namespace AdaptiveCognitiveRehabilitationPlatform.Middleware
{
    /// <summary>
    /// Global exception handling middleware
    /// Catches all unhandled exceptions and returns consistent JSON response
    /// NEVER returns stack traces to client in production
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponseDto
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Timestamp = DateTime.UtcNow
            };

            switch (exception)
            {
                case ArgumentException argEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.ErrorCode = "ARGUMENT_ERROR";
                    response.Message = argEx.Message;
                    break;

                case UnauthorizedAccessException unAuthEx:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response.ErrorCode = "UNAUTHORIZED";
                    response.Message = "You are not authorized to perform this action";
                    break;

                case KeyNotFoundException notFoundEx:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response.ErrorCode = "NOT_FOUND";
                    response.Message = notFoundEx.Message;
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.ErrorCode = "INTERNAL_SERVER_ERROR";
                    response.Message = "An unexpected error occurred";
                    break;
            }

            // Log full exception details (for debugging)
            var logLevel = context.Response.StatusCode switch
            {
                >= 500 => LogLevel.Error,
                >= 400 => LogLevel.Warning,
                _ => LogLevel.Information
            };

            var logger = context.RequestServices.GetRequiredService<ILogger<GlobalExceptionHandlingMiddleware>>();
            logger.Log(logLevel, exception, "An exception occurred. StatusCode: {StatusCode}, Message: {Message}",
                context.Response.StatusCode, exception.Message);

            return context.Response.WriteAsJsonAsync(response);
        }
    }

    /// <summary>
    /// Extension method to register global exception handling middleware
    /// </summary>
    public static class GlobalExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }
    }
}

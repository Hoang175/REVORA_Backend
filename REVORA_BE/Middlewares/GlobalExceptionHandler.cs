using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.Exceptions;
using System.Diagnostics;

namespace REVORA_BE.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

            var (statusCode, title, code, detail) = exception switch
            {
                BusinessException busEx when busEx.ErrorCode != null => (
                    400, 
                    "Business Rule Violation", 
                    busEx.ErrorCode.Value.ToString(), 
                    busEx.Message // Legacy support
                ),
                BaseException appEx => (
                    appEx.StatusCode, 
                    "Business Error", 
                    appEx.Code, 
                    appEx.ClientMessage // Trả về Client Message an toàn
                ),
                _ => (
                    500, 
                    "Internal Server Error", 
                    "InternalError", 
                    "An unexpected error occurred. Please contact support with the trace ID."
                )
            };

            // Log detailed internal message for diagnostics
            if (statusCode >= 500)
            {
                _logger.LogError(exception, 
                    "An error occurred. TraceId: {TraceId}. InternalMessage: {Message}", 
                    traceId, exception.Message);
            }
            else
            {
                _logger.LogWarning(
                    "Business exception occurred. TraceId: {TraceId}. Code: {Code}. Message: {Message}", 
                    traceId, code, exception.Message);
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            };

            // RFC 7807 Extensions
            problemDetails.Extensions.Add("traceId", traceId);
            problemDetails.Extensions.Add("code", code);

            if (exception is BaseException exWithData && exWithData.DataPayload != null)
            {
                problemDetails.Extensions.Add("data", exWithData.DataPayload);
            }

            if (exception is ValidationException valEx && valEx.Errors != null)
            {
                problemDetails.Extensions.Add("errors", valEx.Errors);
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}

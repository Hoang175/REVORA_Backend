namespace REVORA_BE.Exceptions
{
    public abstract class BaseException : Exception
    {
        public string Code { get; }
        public int StatusCode { get; }
        public string ClientMessage { get; }
        public object? DataPayload { get; }

        protected BaseException(
            string clientMessage, 
            string internalMessage, 
            string code, 
            int statusCode,
            object? dataPayload = null) : base(internalMessage)
        {
            ClientMessage = clientMessage;
            Code = code;
            StatusCode = statusCode;
            DataPayload = dataPayload;
        }
    }

    public class UnauthorizedException : BaseException
    {
        public UnauthorizedException(
            string clientMessage,
            string? internalMessage = null,
            string code = "Unauthorized",
            object? data = null) 
            : base(clientMessage, internalMessage ?? clientMessage, code, 401, data) { }
    }

    public class ForbiddenException : BaseException
    {
        public ForbiddenException(
            string clientMessage,
            string? internalMessage = null,
            string code = "Forbidden") 
            : base(clientMessage, internalMessage ?? clientMessage, code, 403) { }
    }

    public class ValidationException : BaseException
    {
        public IDictionary<string, string[]>? Errors { get; }
        public ValidationException(
            string clientMessage,
            IDictionary<string, string[]>? errors = null,
            string? internalMessage = null) 
            : base(clientMessage, internalMessage ?? clientMessage, "ValidationError", 400) 
        {
            Errors = errors;
        }
    }

    public class NotFoundException : BaseException
    {
        public NotFoundException(
            string clientMessage,
            string? internalMessage = null,
            string code = "NotFound") 
            : base(clientMessage, internalMessage ?? clientMessage, code, 404) { }
    }

    public class ConflictException : BaseException
    {
        public ConflictException(
            string clientMessage,
            string? internalMessage = null,
            string code = "Conflict") 
            : base(clientMessage, internalMessage ?? clientMessage, code, 409) { }
    }
}

using System;

namespace REVORA_BE.Exceptions
{
    public enum BusinessErrorCode
    {
        EmailAlreadyExists,
        UsernameAlreadyExists,
        RoleNotFound,
        InvalidCredentials,
        InvalidRefreshToken,
        SessionExpired,
        UserInactive,
        PasswordMismatch,
        RegistrationConflict
    }

    public class BusinessException : BaseException
    {
        public BusinessErrorCode? ErrorCode { get; }

        public BusinessException(
            string clientMessage,
            string? internalMessage = null,
            string code = "BusinessRuleViolation",
            int statusCode = 400)
            : base(clientMessage, internalMessage ?? clientMessage, code, statusCode)
        {
        }

        public BusinessException(BusinessErrorCode errorCode)
            : base(errorCode.ToString(), errorCode.ToString(), errorCode.ToString(), 400)
        {
            ErrorCode = errorCode;
        }

        public BusinessException(BusinessErrorCode errorCode, string message)
            : base(message, message, errorCode.ToString(), 400)
        {
            ErrorCode = errorCode;
        }
    }
}

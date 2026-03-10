namespace ChatApp.Application.Exceptions
{
    // Thrown when a command fails business validation rules.
    // Caught by GlobalExceptionMiddleware and returned as a 400 Bad Request.
    // Named DomainValidationException to avoid shadowing the BCL
    // System.ComponentModel.DataAnnotations.ValidationException.
    public class DomainValidationException : Exception
    {
        public DomainValidationException(string message) : base(message) { }
    }
}

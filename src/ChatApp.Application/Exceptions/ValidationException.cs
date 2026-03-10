namespace ChatApp.Application.Exceptions
{
    // Thrown when a command fails business validation rules.
    // Caught by GlobalExceptionMiddleware and returned as a 400 Bad Request.
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}

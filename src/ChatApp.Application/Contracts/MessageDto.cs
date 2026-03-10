namespace ChatApp.Application.Contracts
{
    public record MessageDto
    {
        public Guid MessageId { get; init; }
        public string SessionId { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public DateTimeOffset SentAt { get; init; }
    }
}

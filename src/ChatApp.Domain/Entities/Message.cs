namespace ChatApp.Domain.Entities
{
    public class Message
    {
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string SessionId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

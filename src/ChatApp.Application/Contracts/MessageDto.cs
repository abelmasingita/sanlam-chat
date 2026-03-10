namespace ChatApp.Application.Contracts
{
    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
    }
}

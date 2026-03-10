namespace ChatApp.Domain.Entities
{
    public class Message
    {
        // Private parameterless constructor for EF Core materialisation.
        private Message() { }

        public Message(string username, string content, string sessionId)
        {
            Username = username;
            Content = content;
            SessionId = sessionId;
        }

        public Guid MessageId { get; private set; } = Guid.NewGuid();
        public string SessionId { get; private set; } = string.Empty;
        public string Username { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public DateTimeOffset SentAt { get; private set; } = DateTimeOffset.UtcNow;
    }
}

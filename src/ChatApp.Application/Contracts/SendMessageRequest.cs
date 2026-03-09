namespace ChatApp.Application.Contracts
{
    public class SendMessageRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}

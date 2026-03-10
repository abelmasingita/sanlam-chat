using ChatApp.Application.Contracts;
using ChatApp.Application.Exceptions;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using MediatR;

namespace ChatApp.Application.Features.Messages.Commands
{
    // Command carries the intent to persist a new message. MediatR routes it to SendMessageHandler.
    public record SendMessageCommand(string Username, string Content, string SessionId) : IRequest<MessageDto>;

    public class SendMessageHandler : IRequestHandler<SendMessageCommand, MessageDto>
    {
        private readonly IMessageRepository _repository;

        public SendMessageHandler(IMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<MessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                throw new ValidationException("Username is required.");
            if (request.Username.Length > 100)
                throw new ValidationException("Username cannot exceed 100 characters.");
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ValidationException("Message content cannot be empty.");
            if (request.Content.Length > 2000)
                throw new ValidationException("Message cannot exceed 2000 characters.");

            var message = new Message
            {
                SessionId = request.SessionId,
                Username = request.Username,
                Content = request.Content
            };

            await _repository.SaveAsync(message);

            return new MessageDto
            {
                MessageId = message.MessageId,
                Username = message.Username,
                Content = message.Content,
                SentAt = message.SentAt
            };
        }
    }
}

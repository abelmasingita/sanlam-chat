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
                throw new DomainValidationException("Username is required.");
            if (request.Username.Length > 100)
                throw new DomainValidationException("Username cannot exceed 100 characters.");
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new DomainValidationException("Message content cannot be empty.");
            if (request.Content.Length > 2000)
                throw new DomainValidationException("Message cannot exceed 2000 characters.");

            var message = new Message
            {
                SessionId = request.SessionId,
                Username = request.Username,
                Content = request.Content
            };

            await _repository.SaveAsync(message, cancellationToken);

            return new MessageDto
            {
                MessageId = message.MessageId,
                SessionId = message.SessionId,
                Username = message.Username,
                Content = message.Content,
                SentAt = message.SentAt
            };
        }
    }
}

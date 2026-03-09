using ChatApp.Application.Contracts;
using ChatApp.Domain.Interfaces;
using MediatR;

namespace ChatApp.Application.Features.Messages.Queries
{
    public record GetRecentMessagesQuery(int Count = 50) : IRequest<IEnumerable<MessageDto>>;

    public class GetRecentMessagesHandler : IRequestHandler<GetRecentMessagesQuery, IEnumerable<MessageDto>>
    {
        private readonly IMessageRepository _repository;

        public GetRecentMessagesHandler(IMessageRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<MessageDto>> Handle(GetRecentMessagesQuery request, CancellationToken cancellationToken)
        {
            var messages = await _repository.GetRecentAsync(request.Count);

            return messages.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                Username = m.Username,
                Content = m.Content,
                SentAt = m.SentAt
            });
        }
    }
}

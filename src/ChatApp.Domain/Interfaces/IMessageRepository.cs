using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces
{
    public interface IMessageRepository
    {
        Task<IEnumerable<Message>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default);
        Task SaveAsync(Message message, CancellationToken cancellationToken = default);
    }
}

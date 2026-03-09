using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ChatApp.Infrastructure.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ChatDbContext _context;
        private readonly ILogger<MessageRepository> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public MessageRepository(ChatDbContext context, ILogger<MessageRepository> logger)
        {
            _context = context;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetry: (exception, delay, attempt, _) =>
                        _logger.LogWarning("Retry {Attempt} after {Delay}ms: {Error}",
                            attempt, delay.TotalMilliseconds, exception.Message));
        }

        public async Task<IEnumerable<Message>> GetRecentAsync(int count = 50)
        {
            return await _retryPolicy.ExecuteAsync(() =>
                _context.Messages
                    .OrderByDescending(m => m.SentAt)
                    .Take(count)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync());
        }

        public async Task SaveAsync(Message message)
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
            });
        }
    }
}

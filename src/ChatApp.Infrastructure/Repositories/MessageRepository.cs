using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
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

            // Retry up to 3 times with exponential backoff (200ms, 400ms, 600ms) for transient
            // PostgreSQL errors only (e.g. connection failures, timeouts). Non-transient exceptions
            // such as validation errors, schema mismatches, or OOM are not retried.
            _retryPolicy = Policy
                .Handle<NpgsqlException>(ex => ex.IsTransient)
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * attempt),
                    onRetry: (exception, delay, attempt, _) =>
                        _logger.LogWarning("Retry {Attempt} after {Delay}ms: {Error}",
                            attempt, delay.TotalMilliseconds, exception.Message));
        }

        public async Task<IEnumerable<Message>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
        {
            // OrderByDescending to get the most recent N rows, then re-sort ascending
            // so messages are returned in chronological order for the UI.
            return await _retryPolicy.ExecuteAsync(ct =>
                _context.Messages
                    .OrderByDescending(m => m.SentAt)
                    .Take(count)
                    .OrderBy(m => m.SentAt)
                    .ToListAsync(ct), cancellationToken);
        }

        public async Task SaveAsync(Message message, CancellationToken cancellationToken = default)
        {
            // Add once outside the retry lambda — the entity is tracked by EF Core after this call.
            // Calling Add inside the retry would attempt to track an already-tracked entity on retry,
            // causing InvalidOperationException.
            _context.Messages.Add(message);
            try
            {
                await _retryPolicy.ExecuteAsync(ct => _context.SaveChangesAsync(ct), cancellationToken);
            }
            catch
            {
                // Detach the entity so the shared DbContext is not left in a dirty Added state
                // after a non-transient failure. Without this, any subsequent operation on the
                // same scoped instance would find a poisoned change tracker.
                _context.Entry(message).State = EntityState.Detached;
                throw;
            }
        }
    }
}

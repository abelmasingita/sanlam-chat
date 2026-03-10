using ChatApp.Application.Features.Messages.Queries;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Moq;

namespace ChatApp.Tests;

public class GetRecentMessagesHandlerTests
{
    private readonly Mock<IMessageRepository> _repo = new();
    private readonly GetRecentMessagesHandler _handler;

    public GetRecentMessagesHandlerTests()
    {
        _handler = new GetRecentMessagesHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsProjectedDtos()
    {
        var messages = new List<Message>
        {
            new() { MessageId = Guid.NewGuid(), SessionId = "s1", Username = "alice", Content = "Hi", SentAt = DateTimeOffset.UtcNow },
            new() { MessageId = Guid.NewGuid(), SessionId = "s2", Username = "bob",   Content = "Hey", SentAt = DateTimeOffset.UtcNow }
        };
        _repo.Setup(r => r.GetRecentAsync(50, CancellationToken.None)).ReturnsAsync(messages);

        var result = (await _handler.Handle(new GetRecentMessagesQuery(), CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("alice", result[0].Username);
        Assert.Equal("s1", result[0].SessionId);
        Assert.Equal("bob", result[1].Username);
        Assert.Equal("s2", result[1].SessionId);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _repo.Setup(r => r.GetRecentAsync(50, CancellationToken.None)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetRecentMessagesQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PassesCountAndCancellationTokenToRepository()
    {
        var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetRecentAsync(25, cts.Token)).ReturnsAsync([]);

        await _handler.Handle(new GetRecentMessagesQuery(25), cts.Token);

        _repo.Verify(r => r.GetRecentAsync(25, cts.Token), Times.Once);
    }
}

using ChatApp.Api.Hubs;
using ChatApp.Application.Contracts;
using ChatApp.Application.Exceptions;
using ChatApp.Application.Features.Messages.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading.RateLimiting;

namespace ChatApp.Tests;

public class ChatHubTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IHubCallerClients> _clients = new();
    private readonly Mock<IClientProxy> _clientProxy = new();
    private readonly Mock<HubCallerContext> _context = new();
    private readonly PartitionedRateLimiter<string> _rateLimiter;
    private readonly ChatHub _hub;

    public ChatHubTests()
    {
        // Generous rate limiter - tests should not be throttled
        _rateLimiter = PartitionedRateLimiter.Create<string, string>(_ =>
            RateLimitPartition.GetNoLimiter("test"));

        _context.Setup(c => c.ConnectionId).Returns("test-conn");
        _clients.Setup(c => c.All).Returns(_clientProxy.Object);
        _clientProxy
            .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hub = new ChatHub(_mediator.Object, NullLogger<ChatHub>.Instance, _rateLimiter)
        {
            Clients = _clients.Object,
            Context = _context.Object
        };
    }

    [Fact]
    public async Task SendMessage_ValidRequest_BroadcastsToAllClients()
    {
        var request = new SendMessageRequest { Username = "alice", Content = "Hello" };
        var dto = new MessageDto { MessageId = Guid.NewGuid(), SessionId = "test-conn", Username = "alice", Content = "Hello", SentAt = DateTimeOffset.UtcNow };
        _mediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        await _hub.SendMessage(request);

        _clientProxy.Verify(p => p.SendCoreAsync(
            "ReceiveMessage",
            It.Is<object[]>(args => args[0] == dto),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_PassesConnectionIdAsSessionId()
    {
        var request = new SendMessageRequest { Username = "alice", Content = "Hello" };
        _mediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new MessageDto());

        await _hub.SendMessage(request);

        _mediator.Verify(m => m.Send(
            It.Is<SendMessageCommand>(c => c.SessionId == "test-conn"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_ValidationException_ThrowsHubException()
    {
        var request = new SendMessageRequest { Username = "", Content = "Hello" };
        _mediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new ValidationException("Username is required."));

        var ex = await Assert.ThrowsAsync<HubException>(() => _hub.SendMessage(request));
        Assert.Equal("Username is required.", ex.Message);
    }

    [Fact]
    public async Task SendMessage_UnhandledException_ThrowsGenericHubException()
    {
        var request = new SendMessageRequest { Username = "alice", Content = "Hello" };
        _mediator.Setup(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("DB exploded"));

        var ex = await Assert.ThrowsAsync<HubException>(() => _hub.SendMessage(request));
        Assert.Equal("Failed to send message. Please try again.", ex.Message);
    }

    [Fact]
    public async Task SendMessage_RateLimitExceeded_ThrowsHubException()
    {
        // Concurrency limiter with 1 permit and no queue — acquire the single permit upfront
        // so the hub's own AcquireAsync call finds nothing available and rejects immediately.
        var stingyLimiter = PartitionedRateLimiter.Create<string, string>(_ =>
            RateLimitPartition.GetConcurrencyLimiter("test", _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1,
                QueueLimit = 0
            }));

        // Hold the only permit so the hub cannot acquire one
        await stingyLimiter.AcquireAsync("test-conn");

        var hub = new ChatHub(_mediator.Object, NullLogger<ChatHub>.Instance, stingyLimiter)
        {
            Clients = _clients.Object,
            Context = _context.Object
        };

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.SendMessage(new SendMessageRequest { Username = "alice", Content = "Hello" }));

        Assert.Equal("Too many messages. Please slow down.", ex.Message);
        _mediator.Verify(m => m.Send(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

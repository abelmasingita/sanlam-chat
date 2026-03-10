using ChatApp.Application.Exceptions;
using ChatApp.Application.Features.Messages.Commands;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Moq;

namespace ChatApp.Tests;

public class SendMessageHandlerTests
{
    private readonly Mock<IMessageRepository> _repo = new();
    private readonly SendMessageHandler _handler;

    public SendMessageHandlerTests()
    {
        _handler = new SendMessageHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesAndReturnsDto()
    {
        var command = new SendMessageCommand("alice", "Hello", "conn-1");

        var dto = await _handler.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.SaveAsync(It.Is<Message>(m =>
            m.Username == "alice" &&
            m.Content == "Hello" &&
            m.SessionId == "conn-1"), CancellationToken.None), Times.Once);

        Assert.Equal("alice", dto.Username);
        Assert.Equal("Hello", dto.Content);
        Assert.Equal("conn-1", dto.SessionId);
        Assert.NotEqual(Guid.Empty, dto.MessageId);
    }

    [Theory]
    [InlineData("", "Hello")]
    [InlineData("   ", "Hello")]
    public async Task Handle_EmptyUsername_ThrowsDomainValidationException(string username, string content)
    {
        var command = new SendMessageCommand(username, content, "conn-1");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _repo.Verify(r => r.SaveAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UsernameTooLong_ThrowsDomainValidationException()
    {
        var command = new SendMessageCommand(new string('a', 101), "Hello", "conn-1");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("alice", "")]
    [InlineData("alice", "   ")]
    public async Task Handle_EmptyContent_ThrowsDomainValidationException(string username, string content)
    {
        var command = new SendMessageCommand(username, content, "conn-1");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ContentTooLong_ThrowsDomainValidationException()
    {
        var command = new SendMessageCommand("alice", new string('a', 2001), "conn-1");

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        var cts = new CancellationTokenSource();
        var command = new SendMessageCommand("alice", "Hello", "conn-1");

        await _handler.Handle(command, cts.Token);

        _repo.Verify(r => r.SaveAsync(It.IsAny<Message>(), cts.Token), Times.Once);
    }
}

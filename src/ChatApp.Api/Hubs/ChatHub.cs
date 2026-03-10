using ChatApp.Application.Contracts;
using ChatApp.Application.Exceptions;
using ChatApp.Application.Features.Messages.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMediator mediator, ILogger<ChatHub> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // Clients invoke this method. The message is persisted via MediatR,
        // then broadcast to all connected clients as a ReceiveMessage event.
        // HubException sends a structured error back to the caller without closing the connection.
        public async Task SendMessage(SendMessageRequest request)
        {
            try
            {
                var dto = await _mediator.Send(
                    new SendMessageCommand(request.Username, request.Content, Context.ConnectionId));

                await Clients.All.SendAsync("ReceiveMessage", dto);
            }
            catch (ValidationException ex)
            {
                throw new HubException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in SendMessage for connection {ConnectionId}",
                    Context.ConnectionId);
                throw new HubException("Failed to send message. Please try again.");
            }
        }
    }
}

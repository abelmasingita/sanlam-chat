using ChatApp.Application.Contracts;
using ChatApp.Application.Features.Messages.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMediator _mediator;

        public ChatHub(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Clients invoke this method. The message is persisted via MediatR,
        // then broadcast to all connected clients as a ReceiveMessage event.
        public async Task SendMessage(SendMessageRequest request)
        {
            var dto = await _mediator.Send(
                new SendMessageCommand(request.Username, request.Content, Context.ConnectionId));

            await Clients.All.SendAsync("ReceiveMessage", dto);
        }
    }
}

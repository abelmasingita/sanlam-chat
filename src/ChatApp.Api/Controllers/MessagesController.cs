using ChatApp.Application.Features.Messages.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MessagesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var messages = await _mediator.Send(new GetRecentMessagesQuery());
            return Ok(messages);
        }
    }
}

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
        public async Task<IActionResult> GetRecent([FromQuery] int count = 50)
        {
            if (count < 1 || count > 100)
                return BadRequest(new { error = "count must be between 1 and 100." });

            var messages = await _mediator.Send(new GetRecentMessagesQuery(count));
            return Ok(messages);
        }
    }
}

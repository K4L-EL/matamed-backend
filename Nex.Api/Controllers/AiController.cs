using Microsoft.AspNetCore.Mvc;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController(IAiChatService aiChatService) : ControllerBase
{
    public record ChatRequest(string Query);
    public record ChatResponse(string Response);

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new { error = "Query is required" });

        var response = await aiChatService.ChatAsync(request.Query);
        return Ok(new ChatResponse(response));
    }
}

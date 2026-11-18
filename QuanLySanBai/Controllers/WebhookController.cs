using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLySanBai.Services;

namespace QuanLySanBai.Controllers;

[ApiController]
[Route("api/webhook")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class WebhookController(WebhookService webhookService) : ControllerBase
{
    [HttpPost("sepay")]
    public async Task<IActionResult> SePay()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync();
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();

        var result = await webhookService.ProcessSePayWebhook(rawBody, authHeader);
        return StatusCode(result.StatusCode, new
        {
            success = result.Success,
            status = result.ProcessingStatus,
            message = result.Message
        });
    }
}

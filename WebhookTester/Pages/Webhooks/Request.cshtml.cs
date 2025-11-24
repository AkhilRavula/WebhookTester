using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;
using WebhookTester.Models;

namespace WebhookTester.Pages.Webhooks
{
    public class RequestModel : PageModel
    {
        private readonly WebhookContext _context;

        public RequestModel(WebhookContext context)
        {
            _context = context;
        }

        public WebhookRequest WebhookRequest { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid webhookId, int requestId)
        {
            var webhook = await _context.WebhookEndpoints.FindAsync(webhookId);
            if (webhook == null) return NotFound();

            if (webhook.HasPassword)
            {
                var authorized = HttpContext.Session.GetString($"Access_{webhookId}");
                if (authorized != "true")
                {
                    return RedirectToPage("./Details", new { id = webhookId });
                }
            }

            var req = await _context.WebhookRequests
                .Include(r => r.Webhook)
                .FirstOrDefaultAsync(m => m.Id == requestId && m.WebhookId == webhookId);

            if (req == null) return NotFound();

            WebhookRequest = req;
            return Page();
        }
    }
}

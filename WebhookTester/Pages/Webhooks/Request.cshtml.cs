using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;
using WebhookTester.Models;
using WebhookTester.Services;

namespace WebhookTester.Pages.Webhooks
{
    public class RequestModel : PageModel
    {
        // SQLite: private readonly WebhookContext _context;
        private readonly S3StorageService _s3Storage;

        public RequestModel(S3StorageService s3Storage)
        {
            _s3Storage = s3Storage;
        }

        public WebhookRequest WebhookRequest { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid webhookId, int requestId)
        {
            // SQLite: var webhook = await _context.WebhookEndpoints.FindAsync(webhookId);
            var webhook = await _s3Storage.GetEndpointAsync(webhookId);
            if (webhook == null) return NotFound();

            if (webhook.HasPassword)
            {
                var authorized = HttpContext.Session.GetString($"Access_{webhookId}");
                if (authorized != "true")
                {
                    return RedirectToPage("./Details", new { id = webhookId });
                }
            }

            // SQLite: var req = await _context.WebhookRequests.Include(r => r.Webhook).FirstOrDefaultAsync(m => m.Id == requestId && m.WebhookId == webhookId);
            var req = await _s3Storage.GetWebhookRequestAsync(webhookId, requestId);

            if (req == null) return NotFound();

            // Populate the Webhook property for the request
            req.Webhook = webhook;
            
            WebhookRequest = req;
            return Page();
        }
    }
}

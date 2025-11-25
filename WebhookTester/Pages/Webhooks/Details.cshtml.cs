using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;
using WebhookTester.Models;
using WebhookTester.Services;

namespace WebhookTester.Pages.Webhooks
{
    public class DetailsModel : PageModel
    {
        // SQLite: private readonly WebhookContext _context;
        private readonly S3StorageService _s3Storage;

        public DetailsModel(S3StorageService s3Storage)
        {
            _s3Storage = s3Storage;
        }

        public WebhookEndpoint Webhook { get; set; } = default!;
        public List<WebhookRequest> Requests { get; set; } = new();

        [BindProperty]
        public string? Password { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null) return NotFound();

            // SQLite: var webhook = await _context.WebhookEndpoints.FirstOrDefaultAsync(m => m.Id == id);
            var webhook = await _s3Storage.GetEndpointAsync(id.Value);
            if (webhook == null) return NotFound();

            Webhook = webhook;

            if (webhook.HasPassword)
            {
                // Check session
                var authorized = HttpContext.Session.GetString($"Access_{id}");
                if (authorized != "true")
                {
                    // Show password prompt
                    return Page();
                }
            }

            // Load requests
            // SQLite: Requests = await _context.WebhookRequests.Where(r => r.WebhookId == id).OrderByDescending(r => r.ReceivedAt).ToListAsync();
            Requests = await _s3Storage.ListRequestsAsync(id.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null) return NotFound();

            // SQLite: var webhook = await _context.WebhookEndpoints.FindAsync(id);
            var webhook = await _s3Storage.GetEndpointAsync(id.Value);
            if (webhook == null) return NotFound();

            Webhook = webhook;

            if (webhook.HasPassword)
            {
                if (string.IsNullOrEmpty(Password))
                {
                    ErrorMessage = "Password is required.";
                    return Page();
                }
                if (Password != webhook.PasswordHash)
                {
                    ErrorMessage = "Invalid password.";
                    return Page();
                }

                // Success
                HttpContext.Session.SetString($"Access_{id}", "true");
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteWebhookAsync(Guid? id)
        {
            if (id == null) return NotFound();
            // SQLite: var webhook = await _context.WebhookEndpoints.FindAsync(id);
            var webhook = await _s3Storage.GetEndpointAsync(id.Value);
            if (webhook == null) return NotFound();

            if (webhook.HasPassword && HttpContext.Session.GetString($"Access_{id}") != "true")
            {
                return RedirectToPage(new { id });
            }

            // SQLite: _context.WebhookEndpoints.Remove(webhook);
            // SQLite: await _context.SaveChangesAsync();
            await _s3Storage.DeleteEndpointAsync(id.Value);

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostClearRequestsAsync(Guid? id)
        {
            if (id == null) return NotFound();
            // SQLite: var webhook = await _context.WebhookEndpoints.FindAsync(id);
            var webhook = await _s3Storage.GetEndpointAsync(id.Value);
            if (webhook == null) return NotFound();

            if (webhook.HasPassword && HttpContext.Session.GetString($"Access_{id}") != "true")
            {
                return RedirectToPage(new { id });
            }

            // SQLite: var requests = await _context.WebhookRequests.Where(r => r.WebhookId == id).ToListAsync();
            // SQLite: _context.WebhookRequests.RemoveRange(requests);
            // SQLite: await _context.SaveChangesAsync();
            
            // S3: Get all requests and delete them
            var requests = await _s3Storage.ListRequestsAsync(id.Value);
            foreach (var request in requests)
            {
                await _s3Storage.DeleteWebhookRequestAsync(id.Value, request.Id);
            }

            return RedirectToPage(new { id });
        }
    }
}

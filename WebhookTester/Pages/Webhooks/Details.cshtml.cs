using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;
using WebhookTester.Models;

namespace WebhookTester.Pages.Webhooks
{
    public class DetailsModel : PageModel
    {
        private readonly WebhookContext _context;

        public DetailsModel(WebhookContext context)
        {
            _context = context;
        }

        public WebhookEndpoint Webhook { get; set; } = default!;
        public List<WebhookRequest> Requests { get; set; } = new();

        [BindProperty]
        public string? Password { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null) return NotFound();

            var webhook = await _context.WebhookEndpoints.FirstOrDefaultAsync(m => m.Id == id);
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
            Requests = await _context.WebhookRequests
                .Where(r => r.WebhookId == id)
                .OrderByDescending(r => r.ReceivedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null) return NotFound();

            var webhook = await _context.WebhookEndpoints.FindAsync(id);
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
            var webhook = await _context.WebhookEndpoints.FindAsync(id);
            if (webhook == null) return NotFound();

            if (webhook.HasPassword && HttpContext.Session.GetString($"Access_{id}") != "true")
            {
                return RedirectToPage(new { id });
            }

            _context.WebhookEndpoints.Remove(webhook);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostClearRequestsAsync(Guid? id)
        {
            if (id == null) return NotFound();
            var webhook = await _context.WebhookEndpoints.FindAsync(id);
            if (webhook == null) return NotFound();

            if (webhook.HasPassword && HttpContext.Session.GetString($"Access_{id}") != "true")
            {
                return RedirectToPage(new { id });
            }

            var requests = await _context.WebhookRequests.Where(r => r.WebhookId == id).ToListAsync();
            _context.WebhookRequests.RemoveRange(requests);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }
    }
}

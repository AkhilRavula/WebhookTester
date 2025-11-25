using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;
using WebhookTester.Models;
using WebhookTester.Services;

namespace WebhookTester.Pages.Webhooks
{
    public class IndexModel : PageModel
    {
        // SQLite: private readonly WebhookContext _context;
        private readonly S3StorageService _s3Storage;

        public IndexModel(S3StorageService s3Storage)
        {
            _s3Storage = s3Storage;
        }

        public IList<WebhookEndpoint> Webhooks { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // SQLite: Webhooks = await _context.WebhookEndpoints.OrderByDescending(w => w.CreatedAt).ToListAsync();
            Webhooks = await _s3Storage.ListEndpointsAsync();
        }
    }
}

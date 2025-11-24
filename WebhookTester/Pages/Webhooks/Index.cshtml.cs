using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;
using WebhookTester.Models;

namespace WebhookTester.Pages.Webhooks
{
    public class IndexModel : PageModel
    {
        private readonly WebhookContext _context;

        public IndexModel(WebhookContext context)
        {
            _context = context;
        }

        public IList<WebhookEndpoint> Webhooks { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Webhooks = await _context.WebhookEndpoints
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }
    }
}

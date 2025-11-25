using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebhookTester.Data;
using WebhookTester.Models;
using WebhookTester.Services;

namespace WebhookTester.Pages
{
    public class IndexModel : PageModel
    {
        // SQLite: private readonly WebhookContext _context;
        private readonly S3StorageService _s3Storage;

        public IndexModel(S3StorageService s3Storage)
        {
            _s3Storage = s3Storage;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [MaxLength(200)]
            [Display(Name = "Description (Optional)")]
            public string? Description { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Password (Optional)")]
            public string? Password { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var webhook = new WebhookEndpoint
            {
                Id = Guid.NewGuid(),
                Description = Input.Description,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(Input.Password))
            {
                webhook.HasPassword = true;
                webhook.PasswordHash = Input.Password;
            }

            // SQLite: _context.WebhookEndpoints.Add(webhook);
            // SQLite: await _context.SaveChangesAsync();
            await _s3Storage.SaveEndpointAsync(webhook);

            return RedirectToPage("/Webhooks/Details", new { id = webhook.Id });
        }
    }
}

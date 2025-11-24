using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using WebhookTester.Data;
using WebhookTester.Models;

namespace WebhookTester.Pages
{
    public class IndexModel : PageModel
    {
        private readonly WebhookContext _context;

        public IndexModel(WebhookContext context)
        {
            _context = context;
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

            _context.WebhookEndpoints.Add(webhook);
            await _context.SaveChangesAsync();

            return RedirectToPage("/Webhooks/Details", new { id = webhook.Id });
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebhookTester.Models
{
    public class WebhookEndpoint
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public bool HasPassword { get; set; }
        public string? PasswordHash { get; set; }
        
        public DateTime? LastRequestAt { get; set; }
        public bool IsActive { get; set; } = true;

        public List<WebhookRequest> Requests { get; set; } = new();
    }
}

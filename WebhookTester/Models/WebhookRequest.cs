using System;
using System.ComponentModel.DataAnnotations;

namespace WebhookTester.Models
{
    public class WebhookRequest
    {
        public int Id { get; set; }
        
        public Guid WebhookId { get; set; }
        public WebhookEndpoint Webhook { get; set; } = null!;

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(10)]
        public string Method { get; set; } = "GET";
        
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public string Headers { get; set; } = "{}"; // JSON
        public string? Body { get; set; }
        public string? ContentType { get; set; }
        public int StatusCodeSentBack { get; set; } = 200;
        public string? ClientIp { get; set; }
        
        public bool IsBodyBase64Encoded { get; set; }
        public bool IsBodyTruncated { get; set; }
    }
}

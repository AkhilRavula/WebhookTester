using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebhookTester.Data;
using WebhookTester.Hubs;
using WebhookTester.Models;

namespace WebhookTester.Controllers
{
    [Route("webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly WebhookContext _context;
        private readonly IHubContext<WebhookHub> _hubContext;

        public WebhookController(WebhookContext context, IHubContext<WebhookHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [Route("{id:guid}")]
        [Route("{id:guid}/{*path}")]
        [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS", "HEAD")]
        public async Task<IActionResult> Receive(Guid id, string? path)
        {
            var endpoint = await _context.WebhookEndpoints.FindAsync(id);
            if (endpoint == null || !endpoint.IsActive)
            {
                return NotFound();
            }

            if (endpoint.HasPassword)
            {
                string? eventSigningSignature = Request.Headers["signature-256"];
                if (string.IsNullOrEmpty(eventSigningSignature))
                {
                    if (Request.Headers.TryGetValue("signature-256", out var headerPwd))
                    {
                        eventSigningSignature = headerPwd.ToString();
                    }
                }

                if (string.IsNullOrEmpty(eventSigningSignature) || CheckForSignature(eventSigningSignature, endpoint.PasswordHash))
                {
                    return Unauthorized("Invalid or missing signature.");
                }
            }

            var requestRecord = new WebhookRequest
            {
                WebhookId = id,
                ReceivedAt = DateTime.UtcNow,
                Method = Request.Method,
                Path = path ?? string.Empty,
                QueryString = Request.QueryString.HasValue ? Request.QueryString.Value : null,
                Headers = JsonSerializer.Serialize(Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),
                ContentType = Request.ContentType,
                ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                StatusCodeSentBack = 200
            };

            // Read body
            using (var memoryStream = new MemoryStream())
            {
                Request.Body.Position = 0;
                await Request.Body.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                
                if (bytes.Length > 1024 * 1024)
                {
                    var truncatedBytes = new byte[1024 * 1024];
                    Array.Copy(bytes, truncatedBytes, 1024 * 1024);
                    bytes = truncatedBytes;
                    requestRecord.IsBodyTruncated = true;
                }

                if (IsBinary(bytes))
                {
                    requestRecord.Body = Convert.ToBase64String(bytes);
                    requestRecord.IsBodyBase64Encoded = true;
                }
                else
                {
                    requestRecord.Body = Encoding.UTF8.GetString(bytes);
                    requestRecord.IsBodyBase64Encoded = false;
                }
            }

            _context.WebhookRequests.Add(requestRecord);
            endpoint.LastRequestAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notify clients
            await _hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveRequest", new 
            {
                Id = requestRecord.Id,
                ReceivedAt = requestRecord.ReceivedAt,
                Method = requestRecord.Method,
                Path = requestRecord.Path,
                StatusCodeSentBack = requestRecord.StatusCodeSentBack,
                Body = requestRecord.IsBodyBase64Encoded ? "(Binary)" : (requestRecord.Body?.Length > 50 ? requestRecord.Body.Substring(0, 50) + "..." : requestRecord.Body),
                IsBodyBase64Encoded = requestRecord.IsBodyBase64Encoded
            });

            return Ok("OK");
        }

        private bool CheckForSignature(string signature, string passwordHash)
        {

            Request.Body.Position = 0;
            using var memoryStream = new MemoryStream();
            Request.Body.CopyTo(memoryStream);
            var bodyBytes = memoryStream.ToArray();

            // Compute HMAC-SHA256 using the endpoint's PasswordHash as the key
            var keyBytes = Encoding.UTF8.GetBytes(passwordHash);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(bodyBytes);
            var computedSignature = "sha256=" + BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            // Compare signatures in a time-constant way
            return !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signature),
                Encoding.UTF8.GetBytes(computedSignature)
            );
        }

        private bool IsBinary(byte[] data)
        {
            // Simple heuristic: if it contains null bytes, treat as binary.
            // Also if it's not valid UTF8, it might be binary, but GetString handles that gracefully usually.
            // We can be more aggressive: if it has many non-printable chars.
            // For now, null byte check is a good start for binary formats like images/protobuf.
            if (data.Any(b => b == 0)) return true;
            return false;
        }
    }
}

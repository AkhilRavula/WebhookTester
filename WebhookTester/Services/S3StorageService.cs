using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;
using WebhookTester.Models;

namespace WebhookTester.Services
{
    public class S3StorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<S3StorageService> _logger;

        public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3StorageService> logger)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:S3BucketName"] ?? "rpx-webhook";
            _logger = logger;
        }

        #region Webhook Endpoints

        // Save webhook endpoint
        public async Task SaveEndpointAsync(WebhookEndpoint endpoint)
        {
            var key = $"endpoints/{endpoint.Id}.json";
            var json = JsonSerializer.Serialize(endpoint, new JsonSerializerOptions { WriteIndented = true });

            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentBody = json,
                ContentType = "application/json"
            });

            _logger.LogInformation("Saved endpoint {EndpointId} to S3", endpoint.Id);
        }

        // Get webhook endpoint
        public async Task<WebhookEndpoint?> GetEndpointAsync(Guid endpointId)
        {
            try
            {
                var key = $"endpoints/{endpointId}.json";
                var response = await _s3Client.GetObjectAsync(_bucketName, key);

                using var reader = new StreamReader(response.ResponseStream);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<WebhookEndpoint>(json);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Endpoint {EndpointId} not found in S3", endpointId);
                return null;
            }
        }

        // List all webhook endpoints
        public async Task<List<WebhookEndpoint>> ListEndpointsAsync()
        {
            var endpoints = new List<WebhookEndpoint>();

            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = "endpoints/"
            });

            foreach (var obj in response.S3Objects)
            {
                if (obj.Key.EndsWith(".json"))
                {
                    try
                    {
                        var getResponse = await _s3Client.GetObjectAsync(_bucketName, obj.Key);
                        using var reader = new StreamReader(getResponse.ResponseStream);
                        var json = await reader.ReadToEndAsync();
                        var endpoint = JsonSerializer.Deserialize<WebhookEndpoint>(json);
                        if (endpoint != null)
                        {
                            endpoints.Add(endpoint);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading endpoint from S3: {Key}", obj.Key);
                    }
                }
            }

            return endpoints.OrderByDescending(e => e.CreatedAt).ToList();
        }

        // Update endpoint (used for LastRequestAt)
        public async Task UpdateEndpointAsync(WebhookEndpoint endpoint)
        {
            await SaveEndpointAsync(endpoint);
        }

        // Delete webhook endpoint
        public async Task DeleteEndpointAsync(Guid endpointId)
        {
            var key = $"endpoints/{endpointId}.json";
            await _s3Client.DeleteObjectAsync(_bucketName, key);
            _logger.LogInformation("Deleted endpoint {EndpointId} from S3", endpointId);
        }

        #endregion

        #region Webhook Requests

        // Save webhook request
        public async Task<int> SaveWebhookRequestAsync(WebhookRequest request)
        {
            // Generate a unique ID if not set
            if (request.Id == 0)
            {
                request.Id = await GetNextRequestIdAsync(request.WebhookId);
            }

            var key = $"requests/{request.WebhookId}/{request.Id}.json";
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });

            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                ContentBody = json,
                ContentType = "application/json"
            });

            _logger.LogInformation("Saved request {RequestId} for endpoint {EndpointId} to S3", request.Id, request.WebhookId);
            return request.Id;
        }

        // Get webhook request by ID
        public async Task<WebhookRequest?> GetWebhookRequestAsync(Guid webhookId, int requestId)
        {
            try
            {
                var key = $"requests/{webhookId}/{requestId}.json";
                var response = await _s3Client.GetObjectAsync(_bucketName, key);

                using var reader = new StreamReader(response.ResponseStream);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<WebhookRequest>(json);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Request {RequestId} for endpoint {EndpointId} not found in S3", requestId, webhookId);
                return null;
            }
        }

        // List all requests for a webhook endpoint
        public async Task<List<WebhookRequest>> ListRequestsAsync(Guid webhookId)
        {
            var requests = new List<WebhookRequest>();

            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = $"requests/{webhookId}/"
            });

            if (response.S3Objects is not null)
            {
                foreach (var obj in response.S3Objects)
                {
                    if (obj.Key.EndsWith(".json"))
                    {
                        try
                        {
                            var getResponse = await _s3Client.GetObjectAsync(_bucketName, obj.Key);
                            using var reader = new StreamReader(getResponse.ResponseStream);
                            var json = await reader.ReadToEndAsync();
                            var request = JsonSerializer.Deserialize<WebhookRequest>(json);
                            if (request != null)
                            {
                                requests.Add(request);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error reading request from S3: {Key}", obj.Key);
                        }
                    }
                }
            }
            return requests.OrderByDescending(r => r.ReceivedAt).ToList();
        }

        // Delete old webhook requests
        public async Task<int> DeleteOldRequestsAsync(DateTime olderThan)
        {
            int deletedCount = 0;
            var allObjects = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = "requests/"
            });

            if (allObjects.S3Objects is not null)
            { 
            foreach (var obj in allObjects.S3Objects)
            {
                if (obj.Key.EndsWith(".json") && obj.LastModified < olderThan)
                {
                    try
                    {
                        await _s3Client.DeleteObjectAsync(_bucketName, obj.Key);
                        deletedCount++;
                        _logger.LogDebug("Deleted old request: {Key}", obj.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting old request from S3: {Key}", obj.Key);
                    }
                }
            }

            _logger.LogInformation("Deleted {Count} old requests from S3", deletedCount);

            }
        return deletedCount;
    }

        // Delete a specific webhook request
        public async Task DeleteWebhookRequestAsync(Guid webhookId, int requestId)
        {
            var key = $"requests/{webhookId}/{requestId}.json";
            await _s3Client.DeleteObjectAsync(_bucketName, key);
            _logger.LogInformation("Deleted request {RequestId} for endpoint {EndpointId} from S3", requestId, webhookId);
        }

        #endregion

        #region Helper Methods

        // Generate next request ID for a webhook
        private async Task<int> GetNextRequestIdAsync(Guid webhookId)
        {
            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = $"requests/{webhookId}/"
            });

            if (response.S3Objects is null || response.S3Objects.Count == 0)
            {
                return 1;
            }

            var maxId = 0;
            foreach (var obj in response.S3Objects)
            {
                var fileName = Path.GetFileNameWithoutExtension(obj.Key);
                if (int.TryParse(fileName, out int id))
                {
                    maxId = Math.Max(maxId, id);
                }
            }

            return maxId + 1;
        }

        #endregion
    }
}

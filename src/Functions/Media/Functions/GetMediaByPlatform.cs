using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;
using System.Web;
using SharedStorage.Services.Media;
using SharedStorage.Models;
using Utils;
using SharedStorage.Extensions;

namespace Functions.Media.Functions;

/// <summary>
/// Azure Function for retrieving media items filtered by platform (TikTok, Instagram, YouTube, etc.)
/// </summary>
public class GetMediaByPlatform
{
    private readonly IMediaItemService _mediaItemService;
    private readonly IAppInsightsLogger<GetMediaByPlatform> _logger;

    public GetMediaByPlatform(
        IMediaItemService mediaItemService,
        IAppInsightsLogger<GetMediaByPlatform> logger)
    {
        _mediaItemService = mediaItemService ?? throw new ArgumentNullException(nameof(mediaItemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// HTTP trigger function to get media items by platform
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <returns>JSON array of media items filtered by platform</returns>
    [Function("GetMediaByPlatform")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("GetMediaByPlatform function called");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var platform = query["platform"];
            var limitStr = query["limit"];
            var offsetStr = query["offset"];

            // Validate required parameter
            if (string.IsNullOrWhiteSpace(platform))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");

                var errorJson = new
                {
                    Success = false,
                    Message = "Platform parameter is required. Valid values: TikTok, Instagram, YouTube, Facebook, LinkedIn, Pinterest, BlobStorage",
                    Error = "Missing required parameter 'platform'"
                };

                await badRequestResponse.WriteStringAsync(JsonSerializer.Serialize(errorJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

                return badRequestResponse;
            }

            // Normalize platform name (case-insensitive)
            platform = platform.ToLowerInvariant() switch
            {
                "tiktok" => "TikTok",
                "instagram" => "Instagram", 
                "youtube" => "YouTube",
                "facebook" => "Facebook",
                "linkedin" => "LinkedIn",
                "pinterest" => "Pinterest",
                "blobstorage" => "BlobStorage",
                _ => platform // Keep as-is if not in our mapping
            };

            var validPlatforms = new[] { "TikTok", "Instagram", "YouTube", "Facebook", "LinkedIn", "Pinterest", "BlobStorage" };
            
            if (!validPlatforms.Contains(platform))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");

                var errorJson = new
                {
                    Success = false,
                    Message = $"Invalid platform '{platform}'. Valid values: {string.Join(", ", validPlatforms)}",
                    Error = "Invalid parameter value"
                };

                await badRequestResponse.WriteStringAsync(JsonSerializer.Serialize(errorJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

                return badRequestResponse;
            }

            int? limit = null;
            int offset = 0;

            if (int.TryParse(limitStr, out var parsedLimit) && parsedLimit > 0)
            {
                limit = Math.Min(parsedLimit, 100); // Cap at 100 for performance
            }

            if (int.TryParse(offsetStr, out var parsedOffset) && parsedOffset > 0)
            {
                offset = parsedOffset;
            }

            // Get media items filtered by platform
            var mediaEntities = await _mediaItemService.GetMediaByPlatformAsync(platform, limit, offset);
            
            // Convert to DTOs for API response
            var mediaDtos = MediaItemMapper.ToDTOs(mediaEntities);

            // Create response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var jsonResponse = new
            {
                Success = true,
                Data = mediaDtos.ToArray(),
                Count = mediaDtos.Count(),
                Platform = platform,
                Message = $"Successfully retrieved {mediaDtos.Count()} media items from {platform}"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(jsonResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            _logger.LogInformation("GetMediaByPlatform completed successfully. Returned {Count} media items from {Platform}", mediaDtos.Count(), platform);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in GetMediaByPlatform: {Message}", ex, ex.Message);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");

            var errorJson = new
            {
                Success = false,
                Message = "An error occurred while retrieving media items by platform",
                Error = ex.Message
            };

            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(errorJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            return errorResponse;
        }
    }
}
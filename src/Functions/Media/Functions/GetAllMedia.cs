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
/// Azure Function for retrieving all media items from various sources
/// </summary>
public class GetAllMedia
{
    private readonly IMediaItemService _mediaItemService;
    private readonly IAppInsightsLogger<GetAllMedia> _logger;

    public GetAllMedia(
        IMediaItemService mediaItemService,
        IAppInsightsLogger<GetAllMedia> logger)
    {
        _mediaItemService = mediaItemService ?? throw new ArgumentNullException(nameof(mediaItemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// HTTP trigger function to get all media items
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <returns>JSON array of all media items</returns>
    [Function("GetAllMedia")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("GetAllMedia function called");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var limitStr = query["limit"];
            var offsetStr = query["offset"];

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

            // Get all media items from the underlying media service
            // This would ideally support pagination, but for now we'll get all and filter
            var allMediaEntities = await _mediaItemService.GetAllMediaAsync(limit, offset);
            
            // Convert to DTOs for API response
            var mediaDtos = MediaItemMapper.ToDTOs(allMediaEntities);

            // Create response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var jsonResponse = new
            {
                Success = true,
                Data = mediaDtos.ToArray(),
                Count = mediaDtos.Count(),
                Message = "Successfully retrieved all media items"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(jsonResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            _logger.LogInformation("GetAllMedia completed successfully. Returned {Count} media items", mediaDtos.Count());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in GetAllMedia: {Message}", ex, ex.Message);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");

            var errorJson = new
            {
                Success = false,
                Message = "An error occurred while retrieving media items",
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
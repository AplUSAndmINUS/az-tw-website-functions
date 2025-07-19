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
/// Azure Function for retrieving media items filtered by medium type (image, video, audio)
/// </summary>
public class GetMediaByMedium
{
    private readonly IMediaItemService _mediaItemService;
    private readonly IAppInsightsLogger<GetMediaByMedium> _logger;

    public GetMediaByMedium(
        IMediaItemService mediaItemService,
        IAppInsightsLogger<GetMediaByMedium> logger)
    {
        _mediaItemService = mediaItemService ?? throw new ArgumentNullException(nameof(mediaItemService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// HTTP trigger function to get media items by medium type
    /// </summary>
    /// <param name="req">HTTP request</param>
    /// <returns>JSON array of media items filtered by medium type</returns>
    [Function("GetMediaByMedium")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("GetMediaByMedium function called");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var mediumType = query["medium"];
            var limitStr = query["limit"];
            var offsetStr = query["offset"];

            // Validate required parameter
            if (string.IsNullOrWhiteSpace(mediumType))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");

                var errorJson = new
                {
                    Success = false,
                    Message = "Medium type parameter is required. Valid values: image, video, audio",
                    Error = "Missing required parameter 'medium'"
                };

                await badRequestResponse.WriteStringAsync(JsonSerializer.Serialize(errorJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));

                return badRequestResponse;
            }

            // Normalize medium type
            mediumType = mediumType.ToLowerInvariant();
            var validMediumTypes = new[] { "image", "video", "audio" };
            
            if (!validMediumTypes.Contains(mediumType))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "application/json");

                var errorJson = new
                {
                    Success = false,
                    Message = $"Invalid medium type '{mediumType}'. Valid values: {string.Join(", ", validMediumTypes)}",
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

            // Get media items filtered by medium type
            var mediaEntities = await _mediaItemService.GetMediaByMediumAsync(mediumType, limit, offset);
            
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
                Medium = mediumType,
                Message = $"Successfully retrieved {mediaDtos.Count()} {mediumType} media items"
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(jsonResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            _logger.LogInformation("GetMediaByMedium completed successfully. Returned {Count} {MediumType} media items", mediaDtos.Count(), mediumType);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in GetMediaByMedium: {Message}", ex, ex.Message);

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");

            var errorJson = new
            {
                Success = false,
                Message = "An error occurred while retrieving media items by medium type",
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
namespace BlogPosts.Functions;

public class GetAllBlogPosts
{
  public GetAllBlogPosts(ILogger<GetAllBlogPosts> logger)
  {
      _logger = logger;
  }

  [Function("GetAllBlogPosts")]
  public async Task<HttpResponseData> Run(
      [HttpTrigger(AuthorizationLevel.Function, "get", Route = "blogposts")] HttpRequestData req)
  {
      _logger.LogInformation("C# HTTP trigger function processed a request.");

      var response = req.CreateResponse(HttpStatusCode.OK);
      response.Headers.Add("Content-Type", "application/json");
      await response.WriteStringAsync("[]");
      return response;
  }
}

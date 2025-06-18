using Microsoft.Extensions.Configuration;

namespace SharedStorage.Environment;

public class DefaultAppMode(IConfiguration config) : IAppMode
{
  private readonly IConfiguration _config = config;

  public bool UseMockStorage
  {
    get
    {
      return _config["USE_MOCK_STORAGE"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }
  }
}

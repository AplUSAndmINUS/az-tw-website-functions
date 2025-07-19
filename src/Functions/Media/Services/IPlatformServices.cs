using SharedStorage.Models;

namespace Functions.Media.Services;

/// <summary>
/// Interface for TikTok media integration
/// </summary>
public interface ITikTokService
{
    Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50);
}

/// <summary>
/// Interface for Instagram media integration
/// </summary>
public interface IInstagramService
{
    Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50);
}

/// <summary>
/// Interface for YouTube media integration
/// </summary>
public interface IYouTubeService
{
    Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50);
}

/// <summary>
/// Interface for Facebook media integration
/// </summary>
public interface IFacebookService
{
    Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50);
}

/// <summary>
/// Interface for LinkedIn media integration
/// </summary>
public interface ILinkedInService
{
    Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50);
}

/// <summary>
/// Interface for Pinterest media integration
/// </summary>
public interface IPinterestService
{
    Task<IEnumerable<MediaItemModel>> GetLatestMediaAsync(int limit = 50);
}
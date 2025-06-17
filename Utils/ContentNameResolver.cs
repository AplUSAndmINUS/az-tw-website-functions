namespace Utils;

using Utils.Constants;

public static class ContentNameResolver
{
  public static string GetBlobContainerName(ContentSections section, AssetType? assetType = null, bool isMockStorage = false)
  {
    var baseName = $"{(isMockStorage ? "mock-" : "")}{section.ToString().ToLowerInvariant()}";

    return assetType switch
    {
      AssetType.Images => $"{baseName}-images",
      AssetType.Media => $"{baseName}-media",
      AssetType.Video => $"{baseName}-video",
      AssetType.Data => $"{baseName}-data",
      null => baseName,
      _ => throw new ArgumentOutOfRangeException(nameof(assetType))
    };
  }

  public static string GetTableName(ContentSections section, AssetType? assetType = null, bool isMockStorage = false)
  {
    var baseName = $"{(isMockStorage ? "mock" : "")}{section.ToString().ToLowerInvariant()}";

    if (assetType is null)
      return baseName;

    var suffix = assetType switch
    {
      AssetType.Images => "imagesmetadata",
      AssetType.Media => "mediametadata",
      AssetType.Video => "videometadata",
      AssetType.Data => "datametadata",
      AssetType.Comments => "comments",
      _ => throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null)
    };

    return $"{baseName}{suffix}";
  }
}
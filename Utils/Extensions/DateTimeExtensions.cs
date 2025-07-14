using System;

namespace Utils.Extensions
{
  /// <summary>
  /// Extension methods for DateTime handling to ensure UTC consistency
  /// </summary>
  public static class DateTimeExtensions
  {
    /// <summary>
    /// Ensures that a DateTime value is in UTC format
    /// </summary>
    /// <param name="dateTime">The DateTime value to ensure is UTC</param>
    /// <returns>The DateTime value converted to or specified as UTC</returns>
    public static DateTime EnsureUtc(this DateTime dateTime)
    {
      // If it's already UTC, return it as-is
      if (dateTime.Kind == DateTimeKind.Utc)
      {
        return dateTime;
      }

      // If it's unspecified, interpret as UTC (common with Azure Table Storage)
      if (dateTime.Kind == DateTimeKind.Unspecified)
      {
        return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
      }

      // If it's Local, convert to UTC
      return dateTime.ToUniversalTime();
    }

    /// <summary>
    /// Creates a new DateTime with the current UTC time
    /// </summary>
    /// <returns>The current time as UTC DateTime</returns>
    public static DateTime UtcNow()
    {
      return DateTime.UtcNow;
    }
  }
}

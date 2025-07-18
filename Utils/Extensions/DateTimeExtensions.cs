using System;

namespace Utils.Extensions
{
  /// <summary>
  /// Extension methods for DateTime handling to ensure UTC consistency
  /// </summary>
  public static class DateTimeExtensions
  {
    // Default date to use when a valid date is required but not provided
    // Azure Table Storage doesn't accept default DateTime value (1/1/0001)
    private static readonly DateTime DefaultFutureDate = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
    /// Ensures that a DateTime value is valid for Azure Table Storage
    /// </summary>
    /// <param name="dateTime">The DateTime value to ensure is valid</param>
    /// <returns>A valid DateTime (DefaultFutureDate if the input is default/MinValue)</returns>
    public static DateTime EnsureValidStorageDate(this DateTime dateTime)
    {
      // If it's the default value, use our predetermined future date
      if (dateTime == default(DateTime) || dateTime == DateTime.MinValue)
      {
        return DefaultFutureDate;
      }

      // Otherwise, ensure it's in UTC format
      return dateTime.EnsureUtc();
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

namespace PpmV2.Infrastructure.Admin.Seeding;

/// <summary>
/// Converts a Berlin wall-clock time into UTC so screenshot times stay readable
/// across the daylight-saving change.
/// </summary>
internal static class DemoClock
{
    private static readonly TimeZoneInfo Berlin = ResolveBerlin();

    public static DateTime AtBerlin(DateTime utcNow, int dayOffset, int hour)
    {
        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var localToday = TimeZoneInfo.ConvertTimeFromUtc(utc, Berlin).Date;
        var wall = DateTime.SpecifyKind(localToday.AddDays(dayOffset).AddHours(hour), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(wall, Berlin);
    }

    private static TimeZoneInfo ResolveBerlin()
    {
        foreach (var id in new[] { "Europe/Berlin", "W. Europe Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz))
                return tz;
        }

        throw new TimeZoneNotFoundException("Europe/Berlin is not available on this machine.");
    }
}

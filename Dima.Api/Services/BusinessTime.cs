using Dima.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Dima.Api.Services;

/// <summary>Calendar rules use an explicit business zone, independent of the host.</summary>
public sealed class BusinessTime(TimeProvider timeProvider, IOptions<BusinessTimeOptions> options)
{
    private readonly TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
    public DateTime Now => At(timeProvider.GetUtcNow());
    public DateTime At(DateTimeOffset instant) => DateTime.SpecifyKind(
        TimeZoneInfo.ConvertTime(instant, zone).DateTime, DateTimeKind.Unspecified);
}

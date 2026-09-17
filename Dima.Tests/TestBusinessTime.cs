using Dima.Api.Configuration;
using Dima.Api.Services;
using Microsoft.Extensions.Options;

namespace Dima.Tests;

internal static class TestBusinessTime
{
    public static BusinessTime Create(TimeProvider clock, string zone = "UTC") =>
        new(clock, Options.Create(new BusinessTimeOptions { TimeZoneId = zone }));
}

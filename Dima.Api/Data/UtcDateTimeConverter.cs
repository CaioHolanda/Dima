using Dima.Core.Common.Time;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dima.Api.Data;

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => UtcInstant.Normalize(value),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

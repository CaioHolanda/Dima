namespace Dima.Api.Models;

public sealed class AdminAuditLog
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string? ActorName { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public long? TargetId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool Succeeded { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}

namespace Dima.Api.Models;

public sealed class UserSession
{
    public Guid Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastActivityUtc { get; set; }
}

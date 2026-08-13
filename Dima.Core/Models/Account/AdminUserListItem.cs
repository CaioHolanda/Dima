namespace Dima.Core.Models.Account;

public class AdminUserListItem
{
    public long Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool IsPremium { get; set; }

    public string? ProductName { get; set; }

    public DateTime? AccessStartsAt { get; set; }

    public DateTime? AccessEndsAt { get; set; }

    public bool IsActive { get; set; }
}
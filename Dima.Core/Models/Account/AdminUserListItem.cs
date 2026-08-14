namespace Dima.Core.Models.Account;

public class AdminUserListItem
{
    public long Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool IsPremium { get; set; }

    public string? ProductName { get; set; }

    public DateTime? AccessStartsAt { get; set; }

    public DateTime? AccessEndsAt { get; set; }
    // Plano já pago para o futuro
    public string? NextProductName { get; set; }
    public DateTime? NextAccessStartsAt { get; set; }
    public DateTime? NextAccessEndsAt { get; set; }
    public bool IsActive { get; set; }

}
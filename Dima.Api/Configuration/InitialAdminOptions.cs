namespace Dima.Api.Configuration;

public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";
    public bool Enabled { get; set; }
    public string Email { get; set; } = string.Empty;
}
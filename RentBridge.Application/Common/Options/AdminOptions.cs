namespace RentBridge.Application.Common.Options;

/// <summary>
/// Bootstrap admin configuration. On startup the app seeds this account
/// when no Admin user exists yet. Only this default admin can create
/// further admin accounts. Production via Admin__* env vars.
/// </summary>
public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FirstName { get; set; } = "System";
    public string LastName { get; set; } = "Admin";
    public string Password { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Phone)
        && !string.IsNullOrWhiteSpace(Password);
}

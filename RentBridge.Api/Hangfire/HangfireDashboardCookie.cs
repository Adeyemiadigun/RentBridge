namespace RentBridge.Api.Hangfire;

/// <summary>
/// Shared definition of the Hangfire dashboard access cookie, used by
/// AuthController (issuance on login/refresh) and
/// AdminDashboardAuthorizationFilter (validation). The cookie carries the
/// access JWT, scoped to the /hangfire path only.
/// </summary>
internal static class HangfireDashboardCookie
{
    public const string Name = "rb_hangfire_admin";
}

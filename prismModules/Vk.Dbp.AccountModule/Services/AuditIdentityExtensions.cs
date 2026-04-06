namespace Vk.Dbp.AccountModule.Services;

internal static class AuditIdentityExtensions
{
    public static int GetAuditUserId(this IUserSession? userSession)
    {
        return userSession?.IsLoggedIn == true ? userSession.UserId : 0;
    }

    public static string GetAuditUsername(this IUserSession? userSession, string fallback = "system")
    {
        if (userSession?.IsLoggedIn != true)
        {
            return fallback;
        }

        return string.IsNullOrWhiteSpace(userSession.Username)
            ? fallback
            : userSession.Username;
    }
}

namespace RaceDay.Web.Helpers;

public static class SessionHelper
{
    private const string UserIdKey = "UserId";
    private const string UserNameKey = "UserName";
    private const string UserRoleKey = "UserRole";
    private const string UserEmailKey = "UserEmail";

    public static void SignIn(this ISession session, int userId, string fullName, string role, string email)
    {
        session.SetInt32(UserIdKey, userId);
        session.SetString(UserNameKey, fullName);
        session.SetString(UserRoleKey, role);
        session.SetString(UserEmailKey, email);
    }

    public static void SignOut(this ISession session) => session.Clear();

    public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);
    public static string? GetUserName(this ISession session) => session.GetString(UserNameKey);
    public static string? GetUserRole(this ISession session) => session.GetString(UserRoleKey);
    public static string? GetUserEmail(this ISession session) => session.GetString(UserEmailKey);
    public static bool IsAuthenticated(this ISession session) => session.GetInt32(UserIdKey).HasValue;
    public static bool IsOrganiser(this ISession session) => session.GetString(UserRoleKey) == "Organiser";
    public static bool IsParticipant(this ISession session) => session.GetString(UserRoleKey) == "Participant";
}

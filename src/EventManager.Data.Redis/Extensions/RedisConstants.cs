namespace EventManager.Data.Redis.Extensions;

public static class RedisConstants
{
    private const string UserKeyByUsername = "eventmanager:invitations:{username}";

    public static string GetUserInvitationsRedisKeyByUsername(string username) =>
        UserKeyByUsername.Replace("{username}", username);
}
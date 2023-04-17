using EventManager.Data.Redis.Models;

namespace EventManager.Data.Redis.Services.Interfaces;

public interface IRedisService
{
    Task<IEnumerable<CachedEventInvitation>> GetUserEventInvitationsByUsername(string username);
    Task<bool> CacheNewUserEventInvitation(CachedEventInvitation invitation);
    Task<bool> DeleteInvitation(string username, string eventId);
    Task<bool> IsUserAlreadyInvited(string username, string eventId);
}
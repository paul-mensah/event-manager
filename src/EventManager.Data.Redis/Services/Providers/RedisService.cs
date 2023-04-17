using EventManager.Data.Redis.Extensions;
using EventManager.Data.Redis.Models;
using EventManager.Data.Redis.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EventManager.Data.Redis.Services.Providers;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisService> _logger;

    public RedisService(IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<IEnumerable<CachedEventInvitation>> GetUserEventInvitationsByUsername(string username)
    {
        try
        {
            string invitationsKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(username);
            var invitationsRedisValue = await _connectionMultiplexer.GetDatabase().HashGetAllAsync(invitationsKey);

            return invitationsRedisValue
                .Select(x => JsonConvert.DeserializeObject<CachedEventInvitation>(x.Value));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured getting user event invitations by username:{username}", username);
            return null;
        }
    }

    public async Task<bool> CacheNewUserEventInvitation(CachedEventInvitation invitation)
    {
        try
        {
            string invitationsKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(invitation.Username);
            bool cachedSuccessfully = await _connectionMultiplexer.GetDatabase()
                .HashSetAsync(
                    key: invitationsKey, 
                    hashField: invitation.EventId,
                    value: JsonConvert.SerializeObject(invitation));
            
            return cachedSuccessfully;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured caching user event invitations by username:{username}\n{user}", 
                invitation.Username, JsonConvert.SerializeObject(invitation, Formatting.Indented));
            
            return false;
        }
    }

    public async Task<bool> DeleteInvitation(string username, string eventId)
    {
        try
        {
            string invitationsKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(username);
            bool deletedSuccessfully = await _connectionMultiplexer.GetDatabase()
                .HashDeleteAsync(
                    key: invitationsKey,
                    hashField: eventId);
            
            return deletedSuccessfully;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured deleting accepted user event invitations by username");
            
            return false;
        }
    }

    public async Task<bool> IsUserAlreadyInvited(string username, string eventId)
    {
        try
        {
            string invitationsKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(username);
            return await _connectionMultiplexer.GetDatabase()
                .HashExistsAsync(invitationsKey, eventId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured checking if user:{username} has already been invited an event:{eventId}",
                username, eventId);

            return false;
        }
    }
}
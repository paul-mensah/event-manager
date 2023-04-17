using EventManager.Core.Domain.EventInvitations;
using EventManager.Core.Models.Filters;
using EventManager.Core.Models.Responses;

namespace EventManager.Core.Repositories;

public interface IEventInvitationRepository : IRepositoryBase<EventInvitation>
{
    Task<PaginatedResponse<EventInvitation>> GetEventInvitations(EventInvitationsFilter filter);
    Task<List<EventInvitation>> GetUserInvitationsPendingApproval(string username);
}
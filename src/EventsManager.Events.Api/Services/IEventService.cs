using EventManager.Core.Models.Filters;
using EventManager.Core.Models.Requests;
using EventManager.Core.Models.Responses;

namespace EventsManager.Events.Api.Services;

public interface IEventService
{
    Task<BaseResponse<PaginatedResponse<EventResponse>>> GetEvents(EventsFilter filter);
    Task<BaseResponse<EventResponse>> GetEventById(string id);
    Task<BaseResponse<EmptyResponse>> DeleteEventById(string id);
    Task<BaseResponse<EventResponse>> CreateEvent(CreateEventRequest request);
    Task<BaseResponse<EventResponse>> AddParticipant(string id, EventParticipantRequest participant);
    Task<BaseResponse<EventResponse>> RemoveParticipant(string id, string username);
}
using EventManager.Core.Domain.EventInvitations;
using EventManager.Core.Domain.Events;
using EventManager.Core.Models.Filters;
using EventManager.Core.Models.Requests;
using EventManager.Core.Models.Responses;
using EventManager.Core.Repositories;
using EventManager.Core.Services;
using EventManager.Data.Redis.Extensions;
using EventManager.Data.Redis.Models;
using EventManager.Data.Redis.Services.Interfaces;
using FluentValidation.Results;
using Mapster;

namespace EventsManager.Invitations.Api.Services;

public class InvitationService : IInvitationService
{
    private readonly IConfiguration _configuration;
    private readonly IEventInvitationRepository _eventInvitationRepository;
    private readonly IProxyHttpService _proxyHttpService;
    private readonly IRedisService _redisService;

    public InvitationService(IEventInvitationRepository eventInvitationRepository,
        IProxyHttpService proxyHttpService,
        IConfiguration configuration,
        IRedisService redisService)
    {
        _eventInvitationRepository = eventInvitationRepository;
        _proxyHttpService = proxyHttpService;
        _configuration = configuration;
        _redisService = redisService;
    }

    public async Task<BaseResponse<EventInvitationResponse>> CreateInvitation(EventInvitationRequest request)
    {
        ValidationResult validationResult = await new EventInvitationRequestValidator().ValidateAsync(request);

        if (!validationResult.IsValid)
            return CommonResponses.ErrorResponse
                .BadRequestResponse<EventInvitationResponse>("Provide all the necessary details");

        // Check if user has been invited awaiting approval
        string userInvitationsKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(request.Username);
        bool userAlreadyInvited = await _redisService.HashExistsAsync(userInvitationsKey, request.EventId);

        if (userAlreadyInvited)
            return CommonResponses.ErrorResponse
                .ConflictErrorResponse<EventInvitationResponse>("User already invited and awaiting approval");

        // Get event details
        string eventApiBaseUrl = _configuration.GetValue<string>("EventsBaseUrl");
        var eventResponse =
            await _proxyHttpService.GetAsync<Event>($"{eventApiBaseUrl}/api/events/{request.EventId}");

        if (!200.Equals(eventResponse.Code))
            return new BaseResponse<EventInvitationResponse>
            {
                Code = eventResponse.Code,
                Message = eventResponse.Message
            };

        // Check if user has already been added to participants
        bool userIsAParticipant = eventResponse.Data.Participants.Any(x => x.Username.Equals(request.Username));

        if (userIsAParticipant)
            return CommonResponses.ErrorResponse
                .ConflictErrorResponse<EventInvitationResponse>("User already part of event participants");

        // Create event invitation
        EventInvitation newEventInvitation = request.Adapt<EventInvitation>();
        newEventInvitation.Title = eventResponse.Data.Title;
        newEventInvitation.Description = eventResponse.Data.Description;

        bool isCreated = await _eventInvitationRepository.AddAsync(newEventInvitation);

        if (!isCreated)
            return CommonResponses.ErrorResponse
                .FailedDependencyErrorResponse<EventInvitationResponse>();

        // Cache new user invitation
        await _redisService.AddToHashSetAsync(
            newEventInvitation.Adapt<CachedEventInvitation>(),
            hashField: newEventInvitation.EventId,
            key: userInvitationsKey);

        return CommonResponses.SuccessResponse
            .CreatedResponse(newEventInvitation.Adapt<EventInvitationResponse>());
    }

    public async Task<BaseResponse<EventInvitationResponse>> GetInvitationById(string id)
    {
        EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

        return eventInvitation != null
            ? CommonResponses.SuccessResponse.OkResponse(eventInvitation.Adapt<EventInvitationResponse>())
            : CommonResponses.ErrorResponse.NotFoundErrorResponse<EventInvitationResponse>("Invitation not found");
    }

    public async Task<BaseResponse<EventInvitationResponse>> DeclineInvitation(string id)
    {
        EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

        if (eventInvitation is null)
            return CommonResponses.ErrorResponse.NotFoundErrorResponse<EventInvitationResponse>(
                "Invitation not found");

        eventInvitation.UpdatedAt = DateTime.UtcNow;
        eventInvitation.IsAccepted = false;

        bool isUpdated = await _eventInvitationRepository.UpdateAsync(eventInvitation);

        if (!isUpdated)
            return CommonResponses.ErrorResponse.FailedDependencyErrorResponse<EventInvitationResponse>();

        // Delete invitation from hash set
        string userInvitationKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(eventInvitation.Username);
        await _redisService.DeleteFromHashSetAsync(userInvitationKey, eventInvitation.EventId);

        return CommonResponses.SuccessResponse
            .OkResponse(eventInvitation.Adapt<EventInvitationResponse>(), "Declined successfully");
    }

    public async Task<BaseResponse<EventInvitationResponse>> AcceptInvitation(string id)
    {
        EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

        if (eventInvitation is null)
            return CommonResponses.ErrorResponse.NotFoundErrorResponse<EventInvitationResponse>(
                "Invitation not found");

        if (eventInvitation.IsAccepted)
            return CommonResponses.ErrorResponse
                .ConflictErrorResponse<EventInvitationResponse>("Event invitation already accepted");

        eventInvitation.UpdatedAt = DateTime.UtcNow;
        eventInvitation.IsAccepted = true;
        eventInvitation.AcceptedDate = DateTime.UtcNow;

        bool isUpdated = await _eventInvitationRepository.UpdateAsync(eventInvitation);

        if (!isUpdated)
            return CommonResponses.ErrorResponse.FailedDependencyErrorResponse<EventInvitationResponse>();

        // Delete invitation from cache
        string userInvitationKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(eventInvitation.Username);
        await _redisService.DeleteFromHashSetAsync(userInvitationKey, eventInvitation.EventId);

        // Get event details
        string eventApiBaseUrl = _configuration.GetValue<string>("EventsBaseUrl");
        var addParticipantToEventResponse = await _proxyHttpService
            .PatchAsync<Event>($"{eventApiBaseUrl}/api/events/{eventInvitation.EventId}/participant", new
            {
                username = eventInvitation.Username,
                email = eventInvitation.Email,
                name = eventInvitation.Name,
                photoUrl = eventInvitation.PhotoUrl
            });

        if (!200.Equals(addParticipantToEventResponse.Code))
            return new BaseResponse<EventInvitationResponse>
            {
                Code = addParticipantToEventResponse.Code,
                Message = addParticipantToEventResponse.Message
            };

        return CommonResponses.SuccessResponse
            .OkResponse(eventInvitation.Adapt<EventInvitationResponse>(), "Accepted successfully");
    }

    public async Task<BaseResponse<EmptyResponse>> DeleteInvitation(string id)
    {
        EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

        if (eventInvitation is null)
            return CommonResponses.ErrorResponse.NotFoundErrorResponse<EmptyResponse>("Invitation not found");

        bool isDeleted = await _eventInvitationRepository.DeleteAsync(eventInvitation);

        if (!isDeleted) return CommonResponses.ErrorResponse.FailedDependencyErrorResponse<EmptyResponse>();

        // Delete invitation from cache
        string userInvitationKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(eventInvitation.Username);
        await _redisService.DeleteFromHashSetAsync(userInvitationKey, eventInvitation.EventId);

        return CommonResponses.SuccessResponse.DeletedResponse();
    }

    public async Task<BaseResponse<IEnumerable<EventInvitationResponse>>> GetUserInvitationsPendingApproval(
        string username)
    {
        // Get user pending invitations from cache
        string userInvitationsKey = RedisConstants.GetUserInvitationsRedisKeyByUsername(username);
        var cachedEventInvitationsList =
            (await _redisService.GetAllAsync<CachedEventInvitation>(userInvitationsKey)).ToList();

        if (cachedEventInvitationsList.Any())
        {
            var eventInvitationResponses = cachedEventInvitationsList
                .Select(x => x.Adapt<EventInvitationResponse>());

            return CommonResponses.SuccessResponse.OkResponse(eventInvitationResponses);
        }

        // Get user pending invitations from database
        var userEventsPendingApprovalList =
            await _eventInvitationRepository.GetUserInvitationsPendingApproval(username);

        return CommonResponses.SuccessResponse
            .OkResponse(userEventsPendingApprovalList.Adapt<IEnumerable<EventInvitationResponse>>());
    }

    public async Task<BaseResponse<PaginatedResponse<EventInvitationResponse>>> GetEventInvitations(
        EventInvitationsFilter filter)
    {
        var paginatedResponse = await _eventInvitationRepository.GetEventInvitations(filter);
        var eventInvitationResponseList = paginatedResponse.Data
            .Select(x => x.Adapt<EventInvitationResponse>()).ToList();

        return CommonResponses.SuccessResponse.OkResponse(new PaginatedResponse<EventInvitationResponse>
        {
            Data = eventInvitationResponseList,
            CurrentPage = paginatedResponse.CurrentPage,
            PageSize = paginatedResponse.PageSize,
            TotalPages = paginatedResponse.TotalPages,
            TotalRecords = paginatedResponse.TotalRecords
        });
    }
}
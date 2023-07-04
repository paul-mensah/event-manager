﻿using EventManager.Core.Domain.EventInvitations;
using EventManager.Core.Domain.Events;
using EventManager.Core.Models.Filters;
using EventManager.Core.Models.Requests;
using EventManager.Core.Models.Responses;
using EventManager.Core.Repositories;
using EventManager.Core.Services;
using EventManager.Data.Redis.Models;
using EventManager.Data.Redis.Services.Interfaces;
using FluentValidation.Results;
using Mapster;
using Newtonsoft.Json;

namespace EventsManager.Invitations.Api.Services;

public class InvitationService : IInvitationService
{
    private readonly ILogger<InvitationService> _logger;
    private readonly IEventInvitationRepository _eventInvitationRepository;
    private readonly IProxyHttpService _proxyHttpService;
    private readonly IConfiguration _configuration;
    private readonly IRedisService _redisService;

    public InvitationService(ILogger<InvitationService> logger,
        IEventInvitationRepository eventInvitationRepository,
        IProxyHttpService proxyHttpService,
        IConfiguration configuration,
        IRedisService redisService)
    {
        _logger = logger;
        _eventInvitationRepository = eventInvitationRepository;
        _proxyHttpService = proxyHttpService;
        _configuration = configuration;
        _redisService = redisService;
    }
    
    public async Task<BaseResponse<EventInvitationResponse>> CreateInvitation(EventInvitationRequest request)
    {
        try
        {
            ValidationResult validationResult = await new EventInvitationRequestValidator().ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return CommonResponses.ErrorResponse
                    .BadRequestResponse<EventInvitationResponse>("Provide all the necessary details");
            }

            // Check if user has been invited awaiting approval
            bool userAlreadyInvited = await _redisService.IsUserAlreadyInvited(request.Username, request.EventId);
            
            if (userAlreadyInvited)
            {
                return CommonResponses.ErrorResponse
                    .ConflictErrorResponse<EventInvitationResponse>("User already invited and awaiting approval");
            }
            
            // Get event details
            string eventApiBaseUrl = _configuration.GetValue<string>("EventsBaseUrl");
            var eventResponse = await _proxyHttpService.GetAsync<Event>($"{eventApiBaseUrl}/api/events/{request.EventId}");

            if (!200.Equals(eventResponse.Code))
            {
                return new BaseResponse<EventInvitationResponse>
                {
                    Code = eventResponse.Code,
                    Message = eventResponse.Message 
                };
            }

            // Check if user has already been added to participants
            bool userIsAParticipant = eventResponse.Data.Participants.Any(x => x.Username.Equals(request.Username));

            if (userIsAParticipant)
            {
                return CommonResponses.ErrorResponse
                    .ConflictErrorResponse<EventInvitationResponse>("User already part of event participants");
            }

            // Create event invitation
            var newEventInvitation = request.Adapt<EventInvitation>();
            newEventInvitation.Title = eventResponse.Data.Title;
            newEventInvitation.Description = eventResponse.Data.Description;

            bool isCreated = await _eventInvitationRepository.AddAsync(newEventInvitation);

            if (!isCreated)
            {
                return CommonResponses.ErrorResponse
                    .FailedDependencyErrorResponse<EventInvitationResponse>();
            }
            
            // Cache new user invitation
            await _redisService.CacheNewUserEventInvitation(newEventInvitation.Adapt<CachedEventInvitation>());

            return CommonResponses.SuccessResponse
                .CreatedResponse(newEventInvitation.Adapt<EventInvitationResponse>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured creating event invitation\n{event}",
                JsonConvert.SerializeObject(request, Formatting.Indented));

            return CommonResponses.ErrorResponse.InternalServerErrorResponse<EventInvitationResponse>();
        }
    }

    public async Task<BaseResponse<EventInvitationResponse>> GetInvitationById(string id)
    {
        try
        {
            EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

            return eventInvitation != null
                ? CommonResponses.SuccessResponse.OkResponse(eventInvitation.Adapt<EventInvitationResponse>())
                : CommonResponses.ErrorResponse.NotFoundErrorResponse<EventInvitationResponse>("Invitation not found");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error getting invitation by id:{invitationId}", id);
            return CommonResponses.ErrorResponse.InternalServerErrorResponse<EventInvitationResponse>();
        }
    }

    public async Task<BaseResponse<EventInvitationResponse>> DeclineInvitation(string id)
    {
        try
        {
            EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

            if (eventInvitation is null)
            {
                return CommonResponses.ErrorResponse.NotFoundErrorResponse<EventInvitationResponse>("Invitation not found");
            }

            eventInvitation.UpdatedAt = DateTime.UtcNow;
            eventInvitation.IsAccepted = false;

            bool isUpdated = await _eventInvitationRepository.UpdateAsync(eventInvitation);

            if (!isUpdated)
            {
                return CommonResponses.ErrorResponse.FailedDependencyErrorResponse<EventInvitationResponse>();
            }

            await _redisService.DeleteInvitation(eventInvitation.Username, eventInvitation.EventId);

            return CommonResponses.SuccessResponse
                .OkResponse(eventInvitation.Adapt<EventInvitationResponse>(), "Declined successfully");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured declining invitation with id:{invitationId}", id);
            return CommonResponses.ErrorResponse.InternalServerErrorResponse<EventInvitationResponse>();
        }
    }
    
    public async Task<BaseResponse<EventInvitationResponse>> AcceptInvitation(string id)
    {
        try
        {
            EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

            if (eventInvitation is null)
            {
                return CommonResponses.ErrorResponse.NotFoundErrorResponse<EventInvitationResponse>("Invitation not found");
            }

            if (eventInvitation.IsAccepted)
            {
                return CommonResponses.ErrorResponse
                    .ConflictErrorResponse<EventInvitationResponse>("Event invitation already accepted");
            }
            
            eventInvitation.UpdatedAt = DateTime.UtcNow;
            eventInvitation.IsAccepted = true;
            eventInvitation.AcceptedDate = DateTime.UtcNow;

            bool isUpdated = await _eventInvitationRepository.UpdateAsync(eventInvitation);

            if (!isUpdated)
            {
                return CommonResponses.ErrorResponse.FailedDependencyErrorResponse<EventInvitationResponse>();
            }

            await _redisService.DeleteInvitation(eventInvitation.Username, eventInvitation.EventId);

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
            {
                return new BaseResponse<EventInvitationResponse>
                {
                    Code = addParticipantToEventResponse.Code,
                    Message = addParticipantToEventResponse.Message
                };
            }
            
            return CommonResponses.SuccessResponse
                .OkResponse(eventInvitation.Adapt<EventInvitationResponse>(), "Accepted successfully");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured accepting invitation with id:{invitationId}", id);
            return CommonResponses.ErrorResponse.InternalServerErrorResponse<EventInvitationResponse>();
        }
    }
    
    public async Task<BaseResponse<EmptyResponse>> DeleteInvitation(string id)
    {
        try
        {
            EventInvitation eventInvitation = await _eventInvitationRepository.GetById(id);

            if (eventInvitation is null)
            {
                return CommonResponses.ErrorResponse.NotFoundErrorResponse<EmptyResponse>("Invitation not found");
            }

            bool isDeleted = await _eventInvitationRepository.DeleteAsync(eventInvitation);

            if (!isDeleted)
            {
                return CommonResponses.ErrorResponse.FailedDependencyErrorResponse<EmptyResponse>();
            }

            await _redisService.DeleteInvitation(eventInvitation.Username, eventInvitation.EventId);

            return CommonResponses.SuccessResponse.DeletedResponse();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured deleting invitation with id:{invitationId}", id);
            return CommonResponses.ErrorResponse.InternalServerErrorResponse<EmptyResponse>();
        }
    }

    public async Task<BaseResponse<IEnumerable<EventInvitationResponse>>> GetUserInvitationsPendingApproval(string username)
    {
        try
        {
            var cachedEventInvitationsList = (await _redisService.GetUserEventInvitationsByUsername(username)).ToList();

            if (cachedEventInvitationsList.Any())
            {
                var eventInvitationResponses = cachedEventInvitationsList
                    .Select(x => x.Adapt<EventInvitationResponse>());

                return CommonResponses.SuccessResponse.OkResponse(eventInvitationResponses);
            }

            var userEventsPendingApprovalList = await _eventInvitationRepository.GetUserInvitationsPendingApproval(username);

            return CommonResponses.SuccessResponse
                .OkResponse(userEventsPendingApprovalList.Adapt<IEnumerable<EventInvitationResponse>>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured getting all user invitations pending approval");
            return CommonResponses.ErrorResponse.InternalServerErrorResponse<IEnumerable<EventInvitationResponse>>();
        }
    }

    public async Task<BaseResponse<PaginatedResponse<EventInvitationResponse>>> GetEventInvitations(EventInvitationsFilter filter)
    {
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured getting all event invitations with filters\n{filter}",
                JsonConvert.SerializeObject(filter, Formatting.Indented));

            return CommonResponses.ErrorResponse
                .InternalServerErrorResponse<PaginatedResponse<EventInvitationResponse>>();
        }
    }
}
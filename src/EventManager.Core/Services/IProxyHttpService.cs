using EventManager.Core.Models.Responses;

namespace EventManager.Core.Services;

public interface IProxyHttpService
{
    Task<BaseResponse<T>> GetAsync<T>(string url) where T : class;
    Task<BaseResponse<T>> PatchAsync<T>(string url, object data) where T : class;
}
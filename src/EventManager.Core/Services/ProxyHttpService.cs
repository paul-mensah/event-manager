using System.Net;
using EventManager.Core.Models.Responses;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventManager.Core.Services;

public class ProxyHttpService : IProxyHttpService
{
    private readonly ILogger<ProxyHttpService> _logger;

    public ProxyHttpService(ILogger<ProxyHttpService> logger)
    {
        _logger = logger;
    }

    public async Task<BaseResponse<T>> GetAsync<T>(string url) where T : class
    {
        try
        {
            IFlurlResponse apiResponse = await url
                .AllowAnyHttpStatus()
                .GetAsync();

            string rawResponse = await apiResponse.ResponseMessage.Content.ReadAsStringAsync();

            _logger.LogDebug("[GET] Response from url\n{apiResponse}", rawResponse);

            return JsonConvert.DeserializeObject<BaseResponse<T>>(rawResponse);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[GET] An error occured getting response from url => {url}", url);
            return GetInternalServerResponse<T>();
        }
    }

    public async Task<BaseResponse<T>> PatchAsync<T>(string url, object data) where T : class
    {
        try
        {
            IFlurlResponse apiResponse = await url
                .AllowAnyHttpStatus()
                .PatchJsonAsync(data);

            string rawResponse = await apiResponse.ResponseMessage.Content.ReadAsStringAsync();

            _logger.LogDebug("[PATCH] Response from url\n{apiResponse} with data\n{data}",
                rawResponse, JsonConvert.SerializeObject(data, Formatting.Indented));

            return JsonConvert.DeserializeObject<BaseResponse<T>>(rawResponse);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[PATCH] An error occured getting response from url => {url} with data\n{data}",
                url, JsonConvert.SerializeObject(data, Formatting.Indented));

            return GetInternalServerResponse<T>();
        }
    }

    private static BaseResponse<T> GetInternalServerResponse<T>()
    {
        return new BaseResponse<T>
        {
            Code = (int)HttpStatusCode.InternalServerError,
            Message = "Something bad happened, try again later"
        };
    }
}
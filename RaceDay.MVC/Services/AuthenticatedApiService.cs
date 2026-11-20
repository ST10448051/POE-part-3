
using System.Net.Http.Json;

namespace RaceDay.MVC.Services;

public class AuthenticatedApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly ApiSessionService _session;

    public AuthenticatedApiService(
        IHttpClientFactory factory,
        ApiSessionService session)
    {
        _factory = factory;
        _session = session;
    }

    public async Task<HttpResponseMessage> GetAsync(
        string endpoint)
    {
        var client = _factory.CreateClient("RaceDayAPI");

        using var request = new HttpRequestMessage(
            HttpMethod.Get, endpoint);

        AddAuthentication(request);

        return await client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(
        string endpoint, T data)
    {
        var client = _factory.CreateClient("RaceDayAPI");

        using var request = new HttpRequestMessage(
            HttpMethod.Post, endpoint);

        request.Content = JsonContent.Create(data);

        AddAuthentication(request);

        return await client.SendAsync(request);
    }

    private void AddAuthentication(
        HttpRequestMessage request)
    {
        var cookie = _session.GetApiCookie();

        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new UnauthorizedAccessException(
                "No API authentication session exists.");
        }

        request.Headers.TryAddWithoutValidation(
            "Cookie", cookie);
    }
}

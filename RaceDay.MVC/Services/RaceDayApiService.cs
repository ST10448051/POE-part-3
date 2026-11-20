
using System.Net.Http.Json;

namespace RaceDay.MVC.Services;

public class RaceDayApiService
{
    private readonly HttpClient _httpClient;

    public RaceDayApiService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory
            .CreateClient("RaceDayAPI");
    }

    public async Task<List<T>> GetListAsync<T>(
        string endpoint)
    {
        return await _httpClient
            .GetFromJsonAsync<List<T>>(endpoint)
            ?? new List<T>();
    }

    public async Task<T?> GetAsync<T>(
        string endpoint)
    {
        return await _httpClient
            .GetFromJsonAsync<T>(endpoint);
    }
}

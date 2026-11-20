
using Microsoft.AspNetCore.Http;

namespace RaceDay.MVC.Services;

public class ApiSessionService
{
    private const string CookieKey = "RaceDayApiCookie";

    private readonly IHttpContextAccessor _contextAccessor;

    public ApiSessionService(
        IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public void SetApiCookie(string cookie)
    {
        _contextAccessor.HttpContext?
            .Session.SetString(CookieKey, cookie);
    }

    public string? GetApiCookie()
    {
        return _contextAccessor.HttpContext?
            .Session.GetString(CookieKey);
    }

    public void Clear()
    {
        _contextAccessor.HttpContext?
            .Session.Remove(CookieKey);
    }
}

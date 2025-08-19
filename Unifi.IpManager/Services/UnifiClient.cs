using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using Unifi.IpManager.Options;
using System.IdentityModel.Tokens.Jwt;
using Spydersoft.Platform.Attributes;
using System.Diagnostics.CodeAnalysis;


namespace Unifi.IpManager.Services;

[ExcludeFromCodeCoverage(Justification = "This class is a service client and does not require unit tests.")]
[DependencyInjection(typeof(IUnifiClient), LifetimeOfService.Singleton)]
public class UnifiClient(
    IOptions<UnifiControllerOptions> options,
    ILogger<UnifiClient> logger) : IUnifiClient
{
    protected UnifiControllerOptions UnifiOptions { get; } = options.Value;
    protected ILogger<UnifiClient> Logger { get; } = logger;

    private CookieJar _cookieJar;

    #region IUnifiClient Implementation

    public string SiteId => UnifiOptions.Site;

    public Url BaseApiUrlV1 => UnifiOptions.IsUnifiOs
        ? UnifiOptions.Url.AppendPathSegments("proxy", "network", "api", "s")
        : UnifiOptions.Url.AppendPathSegments("api", "s");

    public Url BaseApiUrlV2 => UnifiOptions.IsUnifiOs
        ? UnifiOptions.Url.AppendPathSegments("proxy", "network", "v2", "api", "site")
        : UnifiOptions.Url.AppendPathSegments("api", "dns");


    public async Task<ServiceResult<T>> ExecuteRequest<T>(Url url, Func<IFlurlRequest, Task<UniResponse<T>>> apiCall, bool includeCsrf = false)
    {
        var result = new ServiceResult<T>();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

        var csrfToken = GetCsrfToken();

        if (!includeCsrf || !string.IsNullOrWhiteSpace(csrfToken))
        {
            try
            {
                var baseRequest = url.WithCookies(_cookieJar);
                if (includeCsrf)
                {
                    baseRequest = baseRequest.WithHeader("X-Csrf-Token", csrfToken);
                }

                var data = await apiCall(baseRequest);

                if (data.Meta.Rc == UniMeta.ErrorResponse)
                {
                    result.MarkFailed(data.Meta.Msg);
                }
                else
                {
                    result.MarkSuccessful(data.Data);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error executing request: {Message}", e.Message);
                result.MarkFailed(e);
            }
        }
        else
        {
            Logger.LogDebug("CSRF Token is null");
            result.MarkFailed("No CSRF Token Present");
        }
        return result;
    }

    public async Task<ServiceResult<T>> ExecuteRequest<T>(Url url, Func<IFlurlRequest, Task<T>> apiCall, bool includeCsrf = false)
    {
        var result = new ServiceResult<T>();

        if (!await VerifyLogin())
        {
            result.MarkFailed("Login failed.");
            return result;
        }

        var csrfToken = GetCsrfToken();

        if (!includeCsrf || !string.IsNullOrWhiteSpace(csrfToken))
        {
            try
            {
                var baseRequest = url.WithCookies(_cookieJar);
                if (includeCsrf)
                {
                    baseRequest = baseRequest.WithHeader("X-Csrf-Token", csrfToken);
                }

                var data = await apiCall(baseRequest);

                if (data is UniResponse<T> response)
                {
                    if (response.Meta.Rc == UniMeta.ErrorResponse)
                    {
                        result.MarkFailed(response.Meta.Msg);
                    }
                    else
                    {
                        result.MarkSuccessful(response.Data);
                    }
                }
                else
                {
                    result.MarkSuccessful(data);
                }

                result.MarkSuccessful(data);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error executing request: {Message}", e.Message);
                result.MarkFailed(e);
            }
        }
        else
        {
            Logger.LogDebug("CSRF Token is null");
            result.MarkFailed("No CSRF Token Present");
        }
        return result;
    }

    #endregion IUnifiClient Implementation

    private async Task<bool> VerifyLogin()
    {
        if (_cookieJar == null || _cookieJar.Count == 0)
        {
            var credentials = new {
                username = UnifiOptions.Username,
                password = UnifiOptions.Password,
                remember = false,
                strict = true
            };

            try
            {
                var url = UnifiOptions.IsUnifiOs
                    ? UnifiOptions.Url.AppendPathSegments("api", "auth", "login")
                    : UnifiOptions.Url.AppendPathSegments("api", "login");

                _ = await url.WithCookies(out _cookieJar).PostJsonAsync(credentials).ReceiveJson<UniResponse<List<string>>>();
            }
            catch (FlurlHttpException ex)
            {
                var errorResponse = await ex.GetResponseJsonAsync<UniResponse<List<string>>>();
                if (errorResponse.Meta.Rc == UniMeta.ErrorResponse)
                {
                    Logger.LogError("Error logging on to {Url}: {Message}", UnifiOptions.Url, errorResponse.Meta.Msg);
                    return false;
                }
                Logger.LogDebug("Error Logging in: URL - {Url}, UserName - {UserName}, {Password}", UnifiOptions.Url, UnifiOptions.Username, UnifiOptions.Password);
                Logger.LogError(ex, "Error logging in to Unifi Controller");
                return false;
            }
        }
        return true;
    }

    private string GetCsrfToken()
    {
        var csrfToken = _cookieJar.FirstOrDefault(cookie => cookie.Name == "X-Csrf-Token");

        if (csrfToken != null)
        {
            return csrfToken.Value;
        }

        var token = _cookieJar.FirstOrDefault(cookie => cookie.Name == "TOKEN");

        if (token != null)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token.Value) as JwtSecurityToken;
            return jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "csrfToken")?.Value;
        }

        return null;
    }



}

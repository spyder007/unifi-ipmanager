using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;
using NullValueHandling = Newtonsoft.Json.NullValueHandling;
using UnifiRequests = Unifi.IpManager.Models.Unifi.Requests;
using Unifi.IpManager.Options;
using System.IdentityModel.Tokens.Jwt;
using Unifi.IpManager.ExternalServices;


namespace Unifi.IpManager.Services;

public class UnifiBaseService(
    IOptions<UnifiControllerOptions> options,
    ILogger logger)
{
    protected UnifiControllerOptions UnifiOptions { get; } = options.Value;
    protected ILogger Logger { get; } = logger;

    // TODO: Make this private by moving UnifiService calls to "executerequest"
    protected CookieJar _cookieJar;

    protected Url BaseDnsSiteUrl => UnifiOptions.IsUnifiOs
        ? UnifiOptions.Url.AppendPathSegments("proxy", "network", "v2", "api", "site")
        : UnifiOptions.Url.AppendPathSegments("api", "dns");

    protected Url BaseSiteApiUrl => UnifiOptions.IsUnifiOs
        ? UnifiOptions.Url.AppendPathSegments("proxy", "network", "api", "s")
        : UnifiOptions.Url.AppendPathSegments("api", "s");

    protected string SiteId => UnifiOptions.Site;

     


    #region Protected Methods
    protected async Task<ServiceResult<T>> ExecuteRequest<T>(Url url, Func<IFlurlRequest, Task<T>> apiCall, bool includeCsrf = false)
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

    // TODO: Make this private
    protected async Task<bool> VerifyLogin()
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
    
    // TODO: Make this private
    protected string GetCsrfToken()
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

    #endregion Private Methods

}

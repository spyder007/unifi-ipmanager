using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Unifi.IpManager.Models.DTO;
using Unifi.IpManager.Models.Unifi;

namespace Unifi.IpManager.Services;

public interface IUnifiClient
{
    string SiteId { get; }
    Url BaseApiUrlV1 { get; }
    Url BaseApiUrlV2 { get; }
    Task<ServiceResult<T>> ExecuteRequest<T>(Url url, Func<IFlurlRequest, Task<T>> apiCall, bool includeCsrf = false);
    Task<ServiceResult<T>> ExecuteRequest<T>(Url url, Func<IFlurlRequest, Task<UniResponse<T>>> apiCall, bool includeCsrf = false);
}

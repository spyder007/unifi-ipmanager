using System;
using Microsoft.Extensions.DependencyInjection;

namespace Unifi.IpManager.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddUnifiService(this IServiceCollection collection)
        {
            return collection == null
                ? throw new ArgumentNullException(nameof(collection))
                : collection.AddTransient<UnifiService, UnifiService>();
        }
    }
}

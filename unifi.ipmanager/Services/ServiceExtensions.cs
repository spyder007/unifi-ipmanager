using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace unifi.ipmanager.Services
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddUnifiService(this IServiceCollection collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            return collection.AddTransient<UnifiService, UnifiService>();
        }
    }
}

﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Asp.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Unifi.IpManager.Services;
using Unifi.IpManager.Options;
using Serilog;

namespace Unifi.IpManager
{
    public class Startup(IConfiguration configuration)
    {
        public IConfiguration Configuration { get; } = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
#pragma warning disable IDE0058 // Expression value is never used
            services.AddSingleton(() => Configuration)
                .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
            });


            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = Configuration.GetValue<string>("Identity:AuthorityUrl");
                    options.Audience = Configuration.GetValue<string>("Identity:ApiName");

                    options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };

                });

            services.AddMvcCore(options =>
                {
                    options.EnableEndpointRouting = false;
                })
                .AddAuthorization()
                .AddApiExplorer()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });

            var cacheConnection = Configuration.GetConnectionString("RedisCache");
            _ = !string.IsNullOrEmpty(cacheConnection)
                ? services.AddStackExchangeRedisCache(options => options.Configuration = cacheConnection)
                : services.AddDistributedMemoryCache();

            services.Configure<DnsServiceOptions>(Configuration.GetSection(DnsServiceOptions.SectionName));
            services.Configure<UnifiControllerOptions>(Configuration.GetSection(UnifiControllerOptions.SectionName));
            services.Configure<IpOptions>(Configuration.GetSection(IpOptions.SectionName));
            services.AddScoped<IDnsService, DnsService>();
            services.AddScoped<IUnifiService, UnifiService>();
            services.AddScoped<IIpService, IpService>();
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddOpenApiDocument(doc =>
            {
                doc.DocumentName = "Unifi.IpManager";
                doc.Title = "Unifi IP Manager API";
                doc.Description = "API Wrapper for the Unifi Controller";
            });
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                                {
                                    var origins = Configuration.GetSection("AllowedOrigins").Get<string[]>();
                                    Log.Warning("Allowed Origins: {origins}", origins);
                                    builder.WithOrigins(origins)
                                                        .AllowAnyHeader()
                                                        .AllowAnyMethod();
                                });
            });
            services.AddHealthChecks();
#pragma warning restore IDE0058 // Expression value is never used
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
#pragma warning disable IDE0058 // Expression value is never used
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/healthz", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
            app.UseOpenApi();
            app.UseAuthentication();
            app.UseRouting();
            app.UseCors();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
                endpoints.MapControllers());
#pragma warning restore IDE0058 // Expression value is never used
        }
    }
}

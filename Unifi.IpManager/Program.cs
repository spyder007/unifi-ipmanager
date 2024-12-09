using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Spydersoft.Platform.Hosting.Options;
using Spydersoft.Platform.Hosting.StartupExtensions;
using Unifi.IpManager.Options;
using Unifi.IpManager.Services;

var builder = WebApplication.CreateBuilder(args);

AppHealthCheckOptions healthCheckOptions = new();
builder.AddSpydersoftTelemetry(typeof(Program).Assembly)
        .AddSpydersoftSerilog();
healthCheckOptions = builder.AddSpydersoftHealthChecks();

_ = builder.Services.AddSingleton(() => builder.Configuration)
           .AddApiVersioning(options =>
           {
               options.DefaultApiVersion = new ApiVersion(1, 0);
               options.AssumeDefaultVersionWhenUnspecified = true;
           });

bool authInstalled = builder.AddSpydersoftIdentity();

_ = builder.Services.AddMvcCore(options =>
    {
        options.EnableEndpointRouting = false;
    })
    .AddAuthorization()
    .AddApiExplorer()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var cacheConnection = builder.Configuration.GetConnectionString("RedisCache");
_ = !string.IsNullOrEmpty(cacheConnection)
    ? builder.Services.AddStackExchangeRedisCache(options => options.Configuration = cacheConnection)
    : builder.Services.AddDistributedMemoryCache();

_ = builder.Services.Configure<DnsServiceOptions>(builder.Configuration.GetSection(DnsServiceOptions.SectionName));
_ = builder.Services.Configure<UnifiControllerOptions>(builder.Configuration.GetSection(UnifiControllerOptions.SectionName));
_ = builder.Services.Configure<IpOptions>(builder.Configuration.GetSection(IpOptions.SectionName));
_ = builder.Services.AddScoped<IDnsService, DnsService>();
_ = builder.Services.AddScoped<IUnifiService, UnifiService>();
_ = builder.Services.AddScoped<IIpService, IpService>();
_ = builder.Services.AddRouting(options => options.LowercaseUrls = true);

_ = builder.Services.AddOpenApiDocument(doc =>
{
    doc.DocumentName = "Unifi.IpManager";
    doc.Title = "Unifi IP Manager API";
    doc.Description = "API Wrapper for the Unifi Controller";
});
_ = builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
        Log.Warning("Allowed Origins: {Origins}", origins);
        _ = policyBuilder.WithOrigins(origins)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
    });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    _ = app.UseDeveloperExceptionPage();
}

_ = app.UseSpydersoftHealthChecks(healthCheckOptions);

_ = app
    .UseOpenApi()
    .UseAuthentication(authInstalled)
    .UseRouting()
    .UseCors()
    .UseAuthorization(authInstalled);

_ = app.MapControllers();

await app.RunAsync();

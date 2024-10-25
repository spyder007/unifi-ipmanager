using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Unifi.IpManager;

var baseConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(baseConfig)
    .CreateBootstrapLogger();

try
{
    Log.Information("Unifi.IpManager starting.");
    var builder = WebApplication.CreateBuilder(args);

    _ = builder.Host.UseSerilog((context, services, configuration) =>
    {
        _ = configuration.ReadFrom.Configuration(context.Configuration);
    });

    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();
    startup.Configure(app, app.Environment);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unifi.IpManager failed to start.");
}
finally
{
    Log.Information("Unifi.IpManager shut down complete");
    await Log.CloseAndFlushAsync();
}
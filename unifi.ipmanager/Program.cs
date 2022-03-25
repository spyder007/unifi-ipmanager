using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using unifi.ipmanager;

var baseConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(baseConfig)
    .CreateBootstrapLogger();

try
{
    Log.Information("unifi.ipmanager starting.");
    var builder = WebApplication.CreateBuilder(args);
    var config = builder.Configuration;

    // Add environment variables to allow us to override key vault secrets while debugging
    config.AddEnvironmentVariables();

    // re-add user secrets last to override keyvault connection strings
    config.AddUserSecrets<Program>(optional: true);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
        .ReadFrom.Configuration(context.Configuration);
    });

    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);
    var app = builder.Build();
    startup.Configure(app, app.Environment);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Accruent.Authentication.Api.TokenService failed to start.");
}
finally
{
    Log.Information("Accruent.Authentication.Api.TokenService shut down complete");
    Log.CloseAndFlush();
}
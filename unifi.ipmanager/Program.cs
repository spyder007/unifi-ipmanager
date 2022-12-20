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
    
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        _ = configuration.ReadFrom.Configuration(context.Configuration);
    });

    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();
    startup.Configure(app, app.Environment);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "unifi.ipmanager failed to start.");
}
finally
{
    Log.Information("unifi.ipmanager shut down complete");
    Log.CloseAndFlush();
}
using unifi.ipmanager.Controllers;
using unifi.ipmanager.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using unifi.ipmanager.Services;

namespace unifi.ipmanager
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate)
                .CreateLogger();
        
            services.AddSingleton<IConfiguration>(provider => Configuration);
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = ApiVersion.Parse("1.0");
                options.AssumeDefaultVersionWhenUnspecified = true;

                options.Conventions.Controller<ClientController>()
                    .HasApiVersion(ApiVersion.Parse("1.0"));

            });

            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = Configuration.GetValue<string>("Identity:AuthorityUrl");
                    options.RequireHttpsMetadata = false;
                    options.ApiName = Configuration.GetValue<string>("Identity:ApiName");
                });

            services.AddMvcCore(options =>
                {
                    options.EnableEndpointRouting = false;
                })
                .AddAuthorization()
                .AddApiExplorer();

            services.Configure<UnifiControllerOptions>(Configuration.GetSection("UnifiControllerOptions"));
            services.Configure<IpOptions>(Configuration.GetSection("IpOptions"));
            services.AddScoped<IUnifiService, UnifiService>();
            services.AddScoped<IIpService, IpService>();
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddOpenApiDocument(doc =>
            {
                doc.DocumentName = "unifi.ipmanager";
                doc.Title = "Unifi IP Manager API";
                doc.Description = "API Wrapper for the Unifi Controller";
                doc.SerializerSettings = new JsonSerializerSettings
                {
                    
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
            });
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                                {
                                    var origins = Configuration.GetSection("AllowedOrigins").Get<string[]>();
                                    builder.WithOrigins(origins)
                                                        .AllowAnyHeader()
                                                        .AllowAnyMethod();
                                });
            });
            services.AddHealthChecks();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/healthz", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
            app.UseOpenApi();
            app.UseAuthentication();
            app.UseCors();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
                endpoints.MapControllers());

        }
    }
}

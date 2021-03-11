﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Converters;
using Serilog;
using unifi.ipmanager.Services;

namespace unifi.ipmanager
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddUserSecrets<Startup>()
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            //LoggerFactory = loggerFactory;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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

            services.AddMvcCore(options => options.EnableEndpointRouting = false)
                .AddAuthorization()
                .AddApiExplorer();

            services.Configure<UnifiControllerOptions>(Configuration.GetSection("UnifiControllerOptions"));
            services.AddScoped<IUnifiService, UnifiService>();

            services.AddOpenApiDocument();
            services.AddHealthChecks();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHealthChecks("/health", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
            loggerFactory.AddSerilog();
            app.UseOpenApi();
            app.UseAuthentication();
            
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
                endpoints.MapControllers());

        }
    }
}
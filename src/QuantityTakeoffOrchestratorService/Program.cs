// Copyright © Trimble Inc.
// 
// All rights reserved.
// 
// The entire contents of this file is protected by U.S. and
// International Copyright Laws. Unauthorized reproduction,
// reverse-engineering, and distribution of all or any portion of
// the code contained in this file is strictly prohibited and may
// result in severe civil and criminal penalties and will be
// prosecuted to the maximum extent possible under the law.
// 
// CONFIDENTIALITY
// 
// This source code and all resulting intermediate files, as well as the
// application design, are confidential and proprietary trade secrets of
// Trimble Inc.

using Mep.Platform.Authorization.Middleware.Extensions;
using Mep.Platform.Extensions.AspNetCore;
using Mep.Platform.Extensions.AspNetCore.Exceptions;
using Mep.Platform.Extensions.AutoMapper;
using Mep.Platform.Extensions.Azure.AppConfiguration;
using Mep.Platform.Extensions.FluentValidation;
using Mep.Platform.Extensions.MongoDb;
using Mep.Platform.Extensions.Monitoring;
using Mep.Platform.Extensions.Monitoring.Serilog;
using Mep.Platform.Extensions.Serilog;
using Mep.Platform.Extensions.Swashbuckle.FluentValidation;
using Mep.Platform.Extensions.TestContainers;
using Mep.Platform.FeatureFlags.Sdk.Extensions;
using QuantityTakeoffOrchestratorService.Extensions;
using QuantityTakeoffOrchestratorService.Models.Configurations;
using QuantityTakeoffOrchestratorService.Models.Mapping;
using QuantityTakeoffOrchestratorService.Services;

var builder = WebApplication.CreateBuilder(args)
    .UsePlatformAzureAppConfiguration(x =>
    {
        // todo: adjust this to your needs
        x.AddNamespaces("MepAuthorization", "MepAuthentication", "QuantityTakeoff", "Estimating");
        x.AddDomains("Estimating");
    })
    .UsePlatformSerilog()
    .ConfigureSettingsFromAssemblyTypes(true)
    .AddAutoMapperProfile<MapperProfile>()
    .UsePlatformMongoDb()
    .ConfigureAllSettings()
    .TryAddAllServices()
    .ConfigureMassTransit()
    .ConfigureJsonOptions()
    .AddMepAuthorization(x => x.Configure())
    .RegisterMepFeatureFlagsClients()
    .UsePlatformFluentValidation(true)
    .UseValidationProblemDetails()
    .RunSetup(x =>
    {
        x.Services.AddControllers();
        x.Services.AddCors(options =>
        {
            var settingsConfiguration = x.Configuration.GetSection("Settings").Get<SettingsConfiguration>();
            options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithOrigins(settingsConfiguration.AllowedOrigins);
                });
        });

    })
    .AddFluentValidationToSwagger()
    .UsePlatformHealthCheck()
    .UsePlatformMonitoring();

var app = builder.Build();
app
    .UsePathBase("/quantitytakeofforchestrator")
    .UsePlatformSerilogMonitoringTelemetry()
    .UsePlatformExceptionHandler()
    .UseCors("CorsPolicy")
    .UseAuthentication()
    .UseRouting()
    .UseAuthorization()
    .UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapSignalRHubEndpoints();
    })
    .UseStaticFiles()
    .UseSwagger()
    .UseSwaggerUI()
    .UsePlatformHealthCheck();

app.Run();
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

using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using MassTransit;
using Mep.Platform.Authorization.Middleware.Enums;
using Mep.Platform.Authorization.Middleware.Extensions;
using Mep.Platform.Authorization.Middleware.Options;
using Mep.Platform.Extensions.MongoDb.Services;
using Mep.Platform.Models.Settings.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Azure.SignalR;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using QuantityTakeoffOrchestratorService.Models.Configurations;
using QuantityTakeoffOrchestratorService.NotificationHubs;
using QuantityTakeoffOrchestratorService.Processors;
using QuantityTakeoffOrchestratorService.Processors.Interfaces;
using QuantityTakeoffOrchestratorService.Repositories;
using QuantityTakeoffOrchestratorService.Repositories.Interfaces;
using QuantityTakeoffOrchestratorService.Services;
using QuantityTakeoffOrchestratorService.StateMachines;
using QuantityTakeoffOrchestratorService.StateMachines.Consumers;
using quantitytakeoffservice.MassTransitFormatters;
using QuantityTakeoffService.MassTransitContracts;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace QuantityTakeoffOrchestratorService.Extensions;

/// <summary>
///     Extensions for customization of the service pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServiceCustomExtensions
{
    /// <summary>
    ///     Configures the <see cref="MepAuthOptionsBuilder" /> for the service.
    /// </summary>
    /// <remarks>
    ///     This is expected to be reviewed and customized your needs require it. Be sure to look at the
    ///     <see cref="GrantType" /> enum for built-in configurations according to common TID auth flows.
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Examples of the syntax for additions to this method.
    ///     </para>
    ///     <c>
    ///         authorization.AddGrantTypes(GrantType.AuthCode, GrantType.ClientCredentials);
    ///     </c>
    ///     <para>
    ///         Or
    ///     </para>
    ///     <c>
    ///         authOptionsBuilder.AddCustomPolicy("Editor", () => new AuthorizationPolicyBuilder()
    ///         .RequireAuthenticatedUser()
    ///         .RequireAnyMepFeatureFlags("UserAdmin", "LicenseAdmin")
    ///         .RequireAnyMepRole("xs-admin-user", "xs-admin-license")
    ///         .AddAuthenticationSchemes(GrantType.AuthCode)
    ///         .Build());
    ///     </c>
    ///     <para>
    ///         Or a combination. Whatever you want. I'm not your mom.
    ///     </para>
    /// </example>
    /// <param name="authOptionsBuilder">The options to affect.</param>
    public static void Configure(this MepAuthOptionsBuilder authOptionsBuilder)
    {
        authOptionsBuilder.AddCustomPolicy("Editor", () => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAnyMepFeatureFlags("DotNet6Service_CanCreateStudent")
            .AddAuthenticationSchemes(GrantType.AuthCode)
            .Build());

        authOptionsBuilder.AddCustomPolicy("Admin", () => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireMepFeatureFlags("DotNet6Service_CanCreateStudent", "DotNet6Service_CanDeleteStudent")
            .AddAuthenticationSchemes(GrantType.AuthCode)
            .Build());

    }

    /// <summary>
    ///     A custom extension for this service to configure sections for <see cref="Options" />. This is expected to be
    ///     customized.
    /// </summary>
    /// <example>
    ///     <para>
    ///         Try to use the following syntax for additions to this method.
    ///     </para>
    ///     <c>
    ///         webApplicationBuilder.Services.Configure&lt;SampleType&gt;
    ///         (webApplicationBuilder.Configuration.GetSection(nameof(SampleType)));
    ///     </c>
    /// </example>
    /// <param name="webApplicationBuilder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureAllSettings(this WebApplicationBuilder webApplicationBuilder)
    {
        _ = webApplicationBuilder.Services.Configure<AzureServiceBusSettings>(webApplicationBuilder.Configuration.GetSection("AzureServiceBusSettings"));

        // Needed for Telemetry Nuget
        ActivitySource.AddActivityListener(new ActivityListener
        {
            ShouldListenTo = s => s.Name.Equals("Manta.Services.Telemetry", StringComparison.CurrentCultureIgnoreCase),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        });

        webApplicationBuilder!.Services.Configure<ConnectConfig>(webApplicationBuilder.Configuration.GetSection(nameof(ConnectConfig)));
        webApplicationBuilder!.Services.Configure<TrimbleFileServiceConfig>(webApplicationBuilder.Configuration.GetSection(nameof(TrimbleFileServiceConfig)));

        var mongoDbSettings = new MongoDbSettings();
        var mongoDbSettingsSection = webApplicationBuilder.Configuration.GetSection("MongoDbSettings");
        mongoDbSettingsSection.Bind(mongoDbSettings);
        _ = webApplicationBuilder.Services.Configure<MongoDbSettings>(mongoDbSettingsSection);

        return webApplicationBuilder;
    }
        
       

    /// <summary>
    ///     A custom extension for this service to configure JSON serialization preferences for this service. This is expected
    ///     to be reviewed and customized if it does not fit the need.
    /// </summary>
    /// <param name="webApplicationBuilder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureJsonOptions(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.Configure<JsonOptions>(x =>
        {
            x.SerializerOptions.WriteIndented = true;
            x.SerializerOptions.PropertyNamingPolicy = null;
            x.SerializerOptions.PropertyNameCaseInsensitive = true;
            x.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        return webApplicationBuilder;
    }

    /// <summary>
    ///     A custom extension for this service to register dependencies. This is expected to be customized.
    /// </summary>
    /// <example>
    ///     <para>
    ///         Try to use the following syntax for additions to this method.
    ///     </para>
    ///     <c> webApplicationBuilder.Services.TryAddScoped(ISampleService, SampleService); </c>
    /// </example>
    /// <param name="webApplicationBuilder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder TryAddAllServices(this WebApplicationBuilder webApplicationBuilder)
    {
        var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
        ConventionRegistry.Register("camelCase", conventionPack, t => true);

        webApplicationBuilder.Services.AddHttpContextAccessor();

        webApplicationBuilder.Services.AddApiVersioning();
        webApplicationBuilder.Services.AddSwaggerGen();


        //webApplicationBuilder.Services.TryAddSingleton<IStudentRepository, StudentRepository>();
        //webApplicationBuilder.Services.TryAddScoped<IStudentProcessor, StudentProcessor>();
        webApplicationBuilder.Services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<ConnectConfig>>().Value);
        webApplicationBuilder.Services.AddScoped<QuantityTakeoffOrchestratorHub>();
        webApplicationBuilder.Services.TryAddScoped<IModelConversionProcessor, ModelConversionProcessor>();
        webApplicationBuilder.Services.TryAddScoped<ITrimbleFileService, TrimbleFileService>();
        webApplicationBuilder.Services.TryAddScoped<IConnectClientService, ConnectClientService>();
        webApplicationBuilder.Services.TryAddScoped<IModelMetaDataProcessor, ModelMetaDataProcessor>();
        webApplicationBuilder.Services.TryAddScoped<IModelMetaDataRepository, ModelMetaDataRepository>();
        webApplicationBuilder.Services.AddHttpClient("httpClient");

        webApplicationBuilder.Services.AddAzureClients(clientBuilder =>
        {
            var keyName = webApplicationBuilder.Configuration.GetSection("EncryptionVaultConfiguration:EncryptionKeyName").Value;
            var keyVaultUri = webApplicationBuilder.Configuration.GetSection("EncryptionVaultConfiguration:EncryptionVaultUri").Value;
            var fullKeyUri = new Uri(keyVaultUri + keyName);
            clientBuilder.AddCryptographyClient(fullKeyUri);

            var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true,
                ExcludeAzurePowerShellCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCodeCredential = true,
                ExcludeVisualStudioCredential = true,
                ExcludeManagedIdentityCredential = true
            };

            clientBuilder.UseCredential(new DefaultAzureCredential(defaultAzureCredentialOptions));
        });
        webApplicationBuilder.Services.TryAddScoped<IDataProtectionService, DataProtectionService>();
        webApplicationBuilder.Services.TryAddScoped<IAesEncryptionService, AesEncryptionService>();

        if (webApplicationBuilder.Environment.EnvironmentName.Equals("integration", StringComparison.InvariantCultureIgnoreCase))
        {
            return webApplicationBuilder;
        }

        var signalRServiceConnectionString =
            webApplicationBuilder.Configuration.GetSection("SignalRSettings:ConnectionString").Value;
        _ = webApplicationBuilder.Services.AddSignalR().AddAzureSignalR(opts =>
        {
            opts.ConnectionString = signalRServiceConnectionString;
            opts.ServerStickyMode = ServerStickyMode.Required;
        }).AddNewtonsoftJsonProtocol();

        return webApplicationBuilder;
    }

    /// <summary>
    ///     This extension method maps the SignalR hub endpoints for the QuantityTakeoffOrchestratorService.
    /// </summary>
    /// <param name="endpointRouteBuilder"></param>
    public static void MapSignalRHubEndpoints(this IEndpointRouteBuilder endpointRouteBuilder) => endpointRouteBuilder.MapHub<QuantityTakeoffOrchestratorHub>("/quantitytakeoffhub");

    /// <summary>
    ///     Configures mass transit setup, state machines, sagas, activities, request clients
    /// </summary>
    /// <param name="webAppBuilder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureMassTransit(this WebApplicationBuilder webAppBuilder)
    {
        var azureServiceBusConnectionString = webAppBuilder.Configuration
            .GetSection("AzureServiceBusSettings:ConnectionString").Value;

        //configure Mass Transit
        var isUserNamePrefixRequired = webAppBuilder.Configuration
            .GetSection("AzureServiceBusSettings").GetValue<bool>("IsUserBasedTransportNamingEnabled");

        var mongodbSettings = webAppBuilder.Configuration
            .GetSection("MongoDbSettings").Get<MongoDbSettings>();

        if(mongodbSettings == null)
        {
            throw new ConfigurationErrorsException("MongoDbSettings section is not configured properly.");
        }

        ////mass transit queue name formatter for Azure Service Bus localhost development
        //if (isUserNamePrefixRequired)
        //{
        //    webAppBuilder.Services.TryAddSingleton<IEndpointNameFormatter>(_ =>
        //        new UserNameBasedQueueTopologyFormatter());
        //}

        _ = webAppBuilder.Services.AddMassTransit(mt =>
        {
            mt.SetKebabCaseEndpointNameFormatter();

            var processTrimbleModelConsumer = mt.AddConsumer<ProcessTrimbleModelConsumer>();
            processTrimbleModelConsumer.Endpoint(e =>
            {
                e.PrefetchCount = 1;
                e.ConcurrentMessageLimit = 1; // Set specific limit for this consumer
            });

            var mongoDbService = webAppBuilder.Services.BuildServiceProvider().GetRequiredService<IMongoDbService>();
            var mongoDbName = webAppBuilder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value;
            var mongoDatabase = mongoDbService.Client.GetDatabase(mongoDbName);

            // Add the state machine and configure its MongoDB repository
            mt.AddSagaStateMachine<ModelConversionStateMachine, ModelConversionState>()
                .MongoDbRepository(r =>
                {
                    r.DatabaseFactory(_ => mongoDatabase);
                    r.CollectionName = nameof(ModelConversionStateMachine);
                });

            // register and configure an azure service bus as a message broker
            mt.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.Host(azureServiceBusConnectionString);

                //topics and endpoint (queues) custom formatters for Azure Service Bus localhost development
                if (isUserNamePrefixRequired)
                {
                    // configure custom endpoint name formatter to prefix the user name to the queue names
                    mt.SetEndpointNameFormatter(new UserNameBasedQueueTopologyFormatter());

                    // Automatically add UserName header to all messages (for filtering)
                    cfg.ConfigurePublish(x => x.UseExecute(c => { c.Headers.Set("UserName", Environment.UserName); }));

                    // set the local based topic name formatter to prefix topics with "local-"
                    cfg.MessageTopology.SetEntityNameFormatter(
                        new LocalBasedTopicTopologyFormatter(cfg.MessageTopology.EntityNameFormatter));

                    var userName = Environment.UserName;

                    // Create filter rule based on username
                    var rule = new CreateRuleOptions($"user-{userName}", new SqlRuleFilter($"UserName = '{userName}'"));

                    cfg.SubscriptionEndpoint<IProcessTrimBimModel>($"{userName}-{nameof(IProcessTrimBimModel)}",
                        subscriptionConfig =>
                        {
                            subscriptionConfig.Rule = rule;
                            subscriptionConfig.ConcurrentMessageLimit = 1;
                            subscriptionConfig.PrefetchCount = 1;
                            subscriptionConfig.ConfigureConsumer<ProcessTrimbleModelConsumer>(context);
                        });

                    // Prevent auto-configuration of endpoints since we've manually configured them
                    return;
                }

                cfg.ConfigureEndpoints(context);
            });

        });

        return webAppBuilder;
    }
}

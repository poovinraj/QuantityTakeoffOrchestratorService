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

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Mep.Platform.Extensions.MongoDb.Services;
using Mep.Platform.Extensions.TestContainers;
using MongoDB.Driver;

namespace QuantityTakeoffOrchestratorService.Services;

/// <summary>
///     A service that uses docker and test containers as a background service for testing.
/// </summary>
public class DockerService : IHostedService
{
    private readonly List<DockerContainer> _containers = new();
    private readonly IMongoDbService _mongoDbService;

    /// <summary>
    ///     Constructor.
    /// </summary>
    public DockerService(IServiceProvider serviceProvider)
    {
        _mongoDbService = serviceProvider.GetRequiredService<IMongoDbService>();

        var nameId = Guid.NewGuid().ToString()[..5];

        //Note: This always uses the latest version of the mongodb
        //this can be modified as per our need (that is specific version of mongodb)
        //example .WithImage("mongo:6.0.8-rc0-windowsservercore") or WithImage("mongo:7.0-rc")
        var redis = new ContainerBuilder()
              .WithImage("mongo")
              .WithName($"mongo-{nameId}")
              .WithPortBinding("27017")
              .WithExposedPort(27017)
              .WithWaitStrategy(Wait.ForUnixContainer())
              .Build()!;

        _containers.Add((DockerContainer)redis);
    }

    /// <summary>
    ///     running, or not
    /// </summary>
    public bool Running { get; set; }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _containers.StartAllAsync(cancellationToken);
            Running = true;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
        }

        if (!ContainersAreUsed())
        {
            throw new ArgumentException("This configuration is not properly using the Docker test containers");
        }
    }

    /// <summary>
    ///     Stop the service.
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _containers.StopAllAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
        }
        finally
        {
            Running = false;
            await _containers.DisposeAllAsync();
        }
    }

    /// <summary>
    ///     Performs checks to ensure that the dependencies are using the test containers and then performs reset operations on
    ///     them.
    /// </summary>
    public async Task RestartAsync()
    {
        if (!ContainersAreUsed())
        {
            throw new ArgumentException("This configuration is not properly using the Docker test containers");
        }

        var collectionNames = await (await _mongoDbService.Database.ListCollectionNamesAsync()).ToListAsync();

        foreach (var collectionName in collectionNames)
        {
            await _mongoDbService.Database.DropCollectionAsync(collectionName);
        }
    }

    private bool ContainersAreUsed() =>
        _mongoDbService.Client.Settings.Server.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase);
}

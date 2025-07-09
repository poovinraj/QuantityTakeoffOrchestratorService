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

using System;
using System.Net.Http;
using System.Threading.Tasks;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Documents;
using QuantityTakeoffOrchestratorService.Test.Common.Clients;
using QuantityTakeoffOrchestratorService.Test.Common.Fixtures;
using Flurl.Http;
using Mep.Platform.Extensions.MongoDb;
using Mep.Platform.Extensions.MongoDb.Services;
using Mep.Platform.Models.Settings.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace QuantityTakeoffOrchestratorService.SmokeTests.Fixtures;

/// <summary>
///     A fixture for <see cref="Program" /> in smoke tests.
/// </summary>
public class SmokeFixture : ProgramFixture, IAsyncLifetime
{
    private FilterDefinitionBuilder<StudentDocument> _filter = default!;
    private IMongoCollection<StudentDocument> _studentCollection = default!;
    public StudentClient StudentClient { get; set; } = default!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var mongoDbService = Services.GetRequiredService<IMongoDbService>();
        mongoDbService.Database.EnsureCollectionExists<StudentDocument>("student");
        _studentCollection = mongoDbService.Database.GetCollection<StudentDocument>("student");
        _filter = Builders<StudentDocument>.Filter;

        var client = GetHttpClient();
        StudentClient = new StudentClient(new FlurlClient(client));
    }

    public new Task DisposeAsync() => ProgramFixture.DisposeAsync();

    /// <summary>
    ///     Restart logic.
    /// </summary>
    public async Task RestartAsync()
    {
        var filter = _filter.Regex(x => x.Email, new BsonRegularExpression($".*{StudentFixture.PseudoProvider}"));
        await _studentCollection.DeleteManyAsync(filter);
    }

    private HttpClient GetHttpClient()
    {
        if (!TestFixture.IsDeployment)
        {
            return CreateClient();
        }

        var baseUrl = Services.GetRequiredService<IOptions<DeploymentSettings>>().Value.BaseUrl!;
        return new HttpClient { BaseAddress = new Uri(baseUrl) };
    }
}

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

namespace QuantityTakeoffOrchestratorService.IntegrationTests.Fixtures;

using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using Test.Common.Clients;
using Test.Common.Fixtures;
using Xunit;

/// <summary>
///     A fixture for <see cref="Program" /> in integration tests.
/// </summary>
public class IntegrationFixture : ProgramFixture, IAsyncLifetime
{
    private DockerService _dockerService = default!;

    public IntegrationFixture()
    {
        if (TestFixture.IsDeployment)
        {
            TestFixture.SetEnvironment("integration");
        }
    }

    public StudentClient StudentClient { get; set; } = default!;

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _dockerService =
            (Services.GetRequiredService<IEnumerable<IHostedService>>().FirstOrDefault(x => x is DockerService)! as
                DockerService)!;

        var client = CreateClient();
        StudentClient = new StudentClient(new FlurlClient(client));
    }

    public new async Task DisposeAsync()
    {
        await ProgramFixture.DisposeAsync();
    }

    /// <summary>
    ///     Restart logic.
    /// </summary>
    public async Task RestartAsync()
    {
        await _dockerService.RestartAsync();
    }
}
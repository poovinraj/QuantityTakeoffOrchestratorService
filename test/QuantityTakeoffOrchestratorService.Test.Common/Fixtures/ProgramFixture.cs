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

namespace QuantityTakeoffOrchestratorService.Test.Common.Fixtures;

using Mep.Platform.Authentication.Middleware.Extensions;
using Mep.Platform.Authentication.Middleware.Services.Interfaces;
using Mep.Platform.Extensions.Xunit.Logging;
using Mep.Platform.Models.AuthX.Responses;
using Mep.Platform.Models.Settings.Common.Test;
using Mep.Platform.Models.Settings.Platform;
using Mep.Platform.Models.Settings.Platform.Test;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

/// <summary>
///     A fixture for <see cref="Program" /> that performs trimble identity session retrieval for tests, caching, and
///     logging.
/// </summary>
public class ProgramFixture : WebApplicationFactory<Program>
{
    private ICachedSessionService? _cachedSessionService;

    /// <summary>
    ///     Constructor
    /// </summary>
    public ProgramFixture()
    {
        var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLogs");
        ConfigureXunitLogging(logDirectory, 15);
    }

    public AuthSessionResponse AlternateSession { get; set; } = default!;
    public AuthSessionResponse MainSession { get; set; } = default!;

    public new static Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Resolves the required <see cref="ICachedSessionService" /> and gets a session using the provided
    ///     <see cref="TestAccountCredentials" />.
    /// </summary>
    /// <param name="testAccountsAccountCredentials">The credentials to use for authentication</param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<AuthSessionResponse?> GetSessionAsync(TestAccountCredentials? testAccountsAccountCredentials)
    {
        if (testAccountsAccountCredentials == null)
        {
            throw new NullReferenceException($"{typeof(TestAccountCredentials)} are required and cannot be null");
        }

        _cachedSessionService ??= Services.GetRequiredService<ICachedSessionService>();
        var tidV4OpenIdSettings = Services.GetRequiredService<IOptions<TidV4OpenIdSettings>>().Value;

        return await _cachedSessionService.GetSessionAsync(testAccountsAccountCredentials.Username!,
            testAccountsAccountCredentials.Password!, tidV4OpenIdSettings.ConsumerKey!,
            tidV4OpenIdSettings.ConsumerSecret!, tidV4OpenIdSettings.RedirectUrl!);
    }

    public async Task InitializeAsync()
    {
        var testAccounts = Services.GetRequiredService<IOptions<TestAccountsV4>>().Value;
        MainSession = await GetSessionAsync(testAccounts.PrimaryUser) ?? new AuthSessionResponse();
        AlternateSession = await GetSessionAsync(testAccounts.SecondaryUser) ?? new AuthSessionResponse();
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices((_, serviceCollection) =>
        {
            serviceCollection.UseMepAuthenticationCachedSessions();
        });
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var environment = TestFixture.IsDeployment
            ? Environment.GetEnvironmentVariable(TestFixture.AppEnvironmentVariable)
            : "localhost";
        TestFixture.SetEnvironment(environment);
        builder.UseEnvironment(environment);
        return base.CreateHost(builder);
    }

    /// <summary>
    ///     A method for creating the desired logging behavior that should then take effect when a test is run, whether it
    ///     passes or fails.
    /// </summary>
    /// <remarks>
    ///     You can replace the logger generation logic with your own, as long as it gets passed to
    ///     <see cref="XunitLogSink.SetLogger(ILogger)" />.
    /// </remarks>
    /// <param name="logDirectory"></param>
    /// <param name="maxLogsKept"></param>
    private static void ConfigureXunitLogging(string logDirectory, int maxLogsKept)
    {
        if (Directory.Exists(logDirectory) && Directory.GetFiles(logDirectory).Length > maxLogsKept)
        {
            var oldestFiles = Directory.GetFiles(logDirectory)
                .Where(x => x.EndsWith(".log", StringComparison.CurrentCultureIgnoreCase))
                .Select(x => new FileInfo(x))
                .OrderBy(x => x.CreationTime)
                .Take(Directory.GetFiles(logDirectory).Length - maxLogsKept);

            foreach (var file in oldestFiles)
            {
                File.Delete(file.FullName);
            }
        }

        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException(
                "Could not configure the test logging because no log directory was established");
        }

        var fileLocation = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.log");

        var logger = new LoggerConfiguration()
            .WriteTo
            .File(fileLocation, retainedFileCountLimit: 5)
            .CreateLogger();

        XunitLogSink.SetLogger(logger);
    }
}
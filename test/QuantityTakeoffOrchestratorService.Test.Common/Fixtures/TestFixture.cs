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

/// <summary>
///     Configuration information for the tests.
/// </summary>
public static class TestFixture
{
    /// <summary>
    ///     The environment variable for the entire application.
    /// </summary>
    public const string AppEnvironmentVariable = "ASPNETCORE_ENVIRONMENT";

    /// <summary>
    ///     The variable that we can expect to only exist on a deployment but not a development machine.
    /// </summary>
    private const string DeploymentVariable = "bamboo_build_working_directory";

    /// <summary>
    ///     Detects whether this is running on a build deployment. It checks for a common DevOps environment variable and
    ///     ensures that it is neither null nor empty.
    /// </summary>
    public static bool IsDeployment => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DeploymentVariable));

    /// <summary>
    ///     Easy getter for the environment variable.
    /// </summary>
    public static string? GetEnvironment() => Environment.GetEnvironmentVariable(AppEnvironmentVariable);

    /// <summary>
    ///     Easy setter for the environment variable.
    /// </summary>
    /// <param name="environment">What to set the environment to.</param>
    public static void SetEnvironment(string? environment) =>
        Environment.SetEnvironmentVariable(AppEnvironmentVariable, environment);
}

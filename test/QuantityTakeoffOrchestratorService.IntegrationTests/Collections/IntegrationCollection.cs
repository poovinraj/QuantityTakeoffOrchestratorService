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

using QuantityTakeoffOrchestratorService.IntegrationTests.Fixtures;
using Xunit;

namespace QuantityTakeoffOrchestratorService.IntegrationTests.Collections;

/// <summary>
///     Decorate tests classes with the [Collection("IntegrationCollection")] and you can inject
///     <see cref="IntegrationFixture" /> into the constructor if you want access to the pipeline and database.
/// </summary>
/// <remarks>
///     This class has no code, and is never created. Its purpose is simply to be the place to apply
///     <see cref="CollectionDefinitionAttribute" /> and all the <see cref="ICollectionFixture{TFixture}" /> interfaces.
/// </remarks>
[CollectionDefinition("IntegrationCollection")]
public class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
}

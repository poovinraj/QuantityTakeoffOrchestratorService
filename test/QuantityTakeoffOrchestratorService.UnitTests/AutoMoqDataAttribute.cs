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

using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;

namespace QuantityTakeoffOrchestratorService.UnitTests;

/// <summary>
///     An attribute used to mark a test as using AutoMoq features
/// </summary>
public class AutoMoqDataAttribute : AutoDataAttribute
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public AutoMoqDataAttribute() : base(() =>
        new Fixture().Customize(new AutoNSubstituteCustomization { ConfigureMembers = true }))
    {
    }
}
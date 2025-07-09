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

using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Documents;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Requests;
using FluentAssertions;
using Mep.Platform.Extensions.AspNetCore.TestSuite.Services;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.Models;

public class ModelTests
{
    public ModelTests() => _pocoService = new PocoService();

    private readonly PocoService _pocoService;

    [Fact]
    public void AllPocos_Success()
    {
        var types = new[] { typeof(CreateStudent), typeof(UpdateStudent), typeof(StudentDocument) };

        _pocoService.Invoking(x => x.CreateAndUpdatePocosWithValues(types.ToArray())).Should().NotThrow();
    }
}

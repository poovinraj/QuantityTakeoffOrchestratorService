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

using System.Threading.Tasks;
using QuantityTakeoffOrchestratorService.SmokeTests.Fixtures;
using QuantityTakeoffOrchestratorService.Test.Common.Clients;
using QuantityTakeoffOrchestratorService.Test.Common.Fixtures;
using FluentAssertions;
using Mep.Platform.Extensions.DotNet;
using Mep.Platform.Models.AuthX.Responses;
using Xunit;

namespace QuantityTakeoffOrchestratorService.SmokeTests.Controllers;

[Collection("SmokeCollection")]
public class StudentControllerTests : IClassFixture<StudentFixture>, IAsyncLifetime
{
    public StudentControllerTests(SmokeFixture smokeFixture, StudentFixture studentFixture)
    {
        _smokeFixture = smokeFixture;
        _studentFixture = studentFixture;

        _studentClient = smokeFixture.StudentClient;
        _mainSession = smokeFixture.MainSession;
    }

    private readonly AuthSessionResponse _mainSession;
    private readonly StudentClient _studentClient;
    private readonly StudentFixture _studentFixture;
    private readonly SmokeFixture _smokeFixture;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _smokeFixture.RestartAsync();

    [Fact]
    public async void GetAll_Success()
    {
        var createStudents = _studentFixture.CreateStudents.AsList();

        foreach (var createStudent in createStudents)
        {
            var clientJsonReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);
            clientJsonReply.IsSuccess.Should().BeTrue();
        }

        var getReply = await _studentClient.GetAllAsync(_mainSession.AccessToken);
        getReply.IsSuccess.Should().BeTrue();

        getReply.Value.Should().HaveCount(createStudents.Count);
    }

    [Fact]
    public async void GetById_Success()
    {
        var createStudent = _studentFixture.CreateStudentPoco;
        var clientJsonReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);

        clientJsonReply.IsSuccess.Should().BeTrue();

        var getReply = await _studentClient.GetByIdAsync(clientJsonReply.Value!.Id, _mainSession.AccessToken);
        getReply.IsSuccess.Should().BeTrue();

        getReply.Value!.Email.Should().Be(createStudent.Email);
    }
}

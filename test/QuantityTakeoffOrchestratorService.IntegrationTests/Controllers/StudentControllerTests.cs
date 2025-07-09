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

using System.Net;
using QuantityTakeoffOrchestratorService.IntegrationTests.Fixtures;
using QuantityTakeoffOrchestratorService.IntegrationTests.TestCases.StudentController;
using QuantityTakeoffOrchestratorService.Test.Common.Clients;
using QuantityTakeoffOrchestratorService.Test.Common.Fixtures;
using QuantityTakeoffOrchestratorService.Test.Common.Models.Requests;
using FluentAssertions;
using Mep.Platform.Extensions.Http;
using Mep.Platform.Models.AuthX.Responses;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Xunit.Abstractions;

namespace QuantityTakeoffOrchestratorService.IntegrationTests.Controllers;

[Collection("IntegrationCollection")]
public class StudentControllerTests : IClassFixture<StudentFixture>, IAsyncLifetime
{
    public StudentControllerTests(IntegrationFixture integrationFixture, StudentFixture studentFixture,
        ITestOutputHelper testOutputHelper)
    {
        _integrationFixture = integrationFixture;
        _studentFixture = studentFixture;
        _testOutputHelper = testOutputHelper;

        _studentClient = _integrationFixture.StudentClient;
        _mainSession = _integrationFixture.MainSession;
    }

    private readonly IntegrationFixture _integrationFixture;
    private readonly StudentFixture _studentFixture;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly StudentClient _studentClient;
    private readonly AuthSessionResponse _mainSession;

    [Theory]
    [MemberData(nameof(CreateStudentFailures.TestCases), MemberType = typeof(CreateStudentFailures))]
    public async void Create_GeneralFailure(string description, string expectedMessage,
        Action<CreateStudentPoco> setupAction)
    {
        var createStudent = _studentFixture.CreateStudentPoco;
        _testOutputHelper.WriteLine(description);
        setupAction(createStudent);

        var clientJsonReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);

        clientJsonReply.IsSuccess.Should().BeFalse();

        var validationProblemDetails =
            await clientJsonReply.ResponseMessage.ReadContentAsAsync<ValidationProblemDetails>();

        validationProblemDetails!.Errors.SelectMany(x => x.Value).Should().Contain(expectedMessage);
    }

    [Theory]
    [MemberData(nameof(CreateStudentSuccesses.TestCases), MemberType = typeof(CreateStudentSuccesses))]
    public async void Create_Success(string description, Action<CreateStudentPoco> setupAction)
    {
        var createStudent = _studentFixture.CreateStudentPoco;
        _testOutputHelper.WriteLine(description);
        setupAction(createStudent);

        var clientJsonReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);

        clientJsonReply.IsSuccess.Should().BeTrue();

        clientJsonReply.Value?.Email.Should().BeEquivalentTo(createStudent.Email);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _integrationFixture.RestartAsync();

    [Fact]
    public async void DeleteById_Success()
    {
        var createStudent = _studentFixture.CreateStudentPoco;
        var postReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);

        postReply.IsSuccess.Should().BeTrue();

        var deleteReply =
            await _studentClient.DeleteByIdAsync(postReply.Value?.Id, _mainSession.AccessToken);

        deleteReply.IsSuccess.Should().BeTrue();
        deleteReply.ResponseMessage.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async void GetAll_Success()
    {
        var createStudents = _studentFixture.CreateStudents.Take(2);

        foreach (var createStudent in createStudents)
        {
            var postReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);
            postReply.IsSuccess.Should().BeTrue();
        }

        var getReply = await _studentClient.GetAllAsync(_mainSession.AccessToken);

        getReply.IsSuccess.Should().BeTrue();
        getReply.Value?.Count().Should().Be(2);
    }

    [Fact]
    public async void GetById_Success()
    {
        var createStudent = _studentFixture.CreateStudentPoco;

        var postReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);
        postReply.IsSuccess.Should().BeTrue();

        var getReply = await _studentClient.GetByIdAsync(postReply.Value?.Id, _mainSession.AccessToken);

        getReply.IsSuccess.Should().BeTrue();
        getReply.Value?.Email.Should().Be(createStudent.Email);
    }

    [Fact]
    public async void Update_Success()
    {
        var createStudent = _studentFixture.CreateStudentPoco;

        var postReply = await _studentClient.CreateAsync(createStudent, _mainSession.AccessToken);

        postReply.IsSuccess.Should().BeTrue();

        var updateStudent = _studentFixture.UpdateStudentPoco;
        updateStudent.Id = postReply.Value?.Id!;

        var updateReply = await _studentClient.UpdateAsync(updateStudent, _mainSession.AccessToken);

        updateReply.IsSuccess.Should().BeTrue();
        updateReply.Value?.Email.Should().Be(updateStudent.Email);
    }
}

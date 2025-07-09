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

using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Requests;
using QuantityTakeoffOrchestratorService.Controllers.Students.v1;
using QuantityTakeoffOrchestratorService.Processor.Interfaces;
using QuantityTakeoffOrchestratorService.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.Controllers;

public class StudentControllerTests : IDisposable
{
    public StudentControllerTests()
    {
        _studentFixture = new StudentFixture();
        _fixture = new Fixture();
        _fixture.Customize(new AutoNSubstituteCustomization());
        _mockProcessor = _fixture.Freeze<IStudentProcessor>();
        _studentController = new StudentController(_mockProcessor);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private readonly StudentFixture _studentFixture;
    private readonly Fixture _fixture;
    private readonly IStudentProcessor _mockProcessor;
    private readonly StudentController _studentController;
    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
            try
            {
                _studentController.Dispose();
            }
            catch (Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }

        _disposed = true;
    }

    [Fact]
    public async Task Create_Returns_CreatedAtAction()
    {
        var student = _studentFixture.Student;
        var createStudent = _studentFixture.CreateStudent;

        _mockProcessor.CreateAsync(Arg.Any<CreateStudent>()).Returns(student);

        var result = await _studentController.Create(createStudent);

        await _mockProcessor.Received(1).CreateAsync(Arg.Any<CreateStudent>());

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var value = (CreatedAtActionResult)result.Result!;
        value.ActionName.Should().Be(nameof(_studentController.GetById));
        value.RouteValues?["id"].Should().Be(student.Id);
        value.Value.Should().Be(student);
    }

    [Fact]
    // Rewrite the above method to use NSubstitute instead of Moq
    public async Task Create_Returns_NotFound()
    {
        var createStudent = _studentFixture.CreateStudent;

        _mockProcessor.CreateAsync(Arg.Any<CreateStudent>()).ReturnsNull();

        var result = await _studentController.Create(createStudent);

        await _mockProcessor.Received(1).CreateAsync(Arg.Any<CreateStudent>());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteById_Returns_NoContent()
    {
        var id = _fixture.Create<string>();

        _mockProcessor.DeleteByIdAsync(Arg.Any<string>()).Returns(true);

        var result = await _studentController.DeleteById(id);

        await _mockProcessor.Received(1).DeleteByIdAsync(Arg.Any<string>());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteById_Returns_NotFound()
    {
        var id = _fixture.Create<string>();

        _mockProcessor.DeleteByIdAsync(Arg.Any<string>()).Returns(false);

        var result = await _studentController.DeleteById(id);

        await _mockProcessor.Received(1).DeleteByIdAsync(Arg.Any<string>());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_Returns_NotFound()
    {
        _mockProcessor.GetAllAsync().ReturnsNull();

        var result = await _studentController.GetAll();

        await _mockProcessor.Received(1).GetAllAsync();

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_Returns_Ok()
    {
        var students = _studentFixture.Students;

        _mockProcessor.GetAllAsync().Returns(students);

        var result = await _studentController.GetAll();

        await _mockProcessor.Received(1).GetAllAsync();

        result.Result.Should().BeOfType<OkObjectResult>();
        var value = (OkObjectResult)result.Result!;
        value.Value.Should().Be(students);
    }

    [Fact]
    public async Task GetById_Returns_NotFound()
    {
        var id = _fixture.Create<string>();

        _mockProcessor.GetByIdAsync(Arg.Any<string>()).ReturnsNull();

        var result = await _studentController.GetById(id);

        await _mockProcessor.Received(1).GetByIdAsync(Arg.Any<string>());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_Returns_Ok()
    {
        var id = _fixture.Create<string>();
        var student = _studentFixture.Student;

        _mockProcessor.GetByIdAsync(Arg.Any<string>()).Returns(student);

        var result = await _studentController.GetById(id);

        await _mockProcessor.Received(1).GetByIdAsync(Arg.Any<string>());

        result.Result.Should().BeOfType<OkObjectResult>();
        var value = (OkObjectResult)result.Result!;
        value.Value.Should().Be(student);
    }

    [Fact]
    public async Task Update_Returns_NotFound()
    {
        var updateStudent = _studentFixture.UpdateStudent;

        _mockProcessor.UpdateAsync(Arg.Any<UpdateStudent>()).ReturnsNull();

        var result = await _studentController.Update(updateStudent);

        await _mockProcessor.Received(1).UpdateAsync(Arg.Any<UpdateStudent>());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_Returns_Ok()
    {
        var student = _studentFixture.Student;
        var updateStudent = _studentFixture.UpdateStudent;

        _mockProcessor.UpdateAsync(Arg.Any<UpdateStudent>()).Returns(student);

        var result = await _studentController.Update(updateStudent);

        await _mockProcessor.Received(1).UpdateAsync(Arg.Any<UpdateStudent>());

        result.Result.Should().BeOfType<OkObjectResult>();
        var value = (OkObjectResult)result.Result!;
        value.Value.Should().Be(student);
    }
}
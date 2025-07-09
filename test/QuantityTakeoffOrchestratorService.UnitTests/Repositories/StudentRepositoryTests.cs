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
using AutoFixture.Xunit2;
using AutoMapper;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Documents;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Requests;
using QuantityTakeoffOrchestratorService.Repositories;
using QuantityTakeoffOrchestratorService.Repositories.Interfaces;
using QuantityTakeoffOrchestratorService.UnitTests.Fixtures;
using FluentAssertions;
using Mep.Platform.Extensions.MongoDb.Services;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace QuantityTakeoffOrchestratorService.UnitTests.Repositories;

/// <summary>
///     This shows how to test an <see cref="IStudentRepository" /> if that is desired. But relying upon
///     integration tests to test repositories is valid. Only use these patterns if you feel it necessary.
/// </summary>
public class StudentRepositoryTests
{
    private readonly Fixture _fixture;
    private readonly StudentFixture _studentFixture;

    public StudentRepositoryTests()
    {
        _studentFixture = new StudentFixture();
        _fixture = new Fixture();
    }

    [Theory]
    [AutoMoqData]
    public async Task CreateAsync_Returns_Student([Frozen] IMongoDbService mongoService, [Frozen] IMapper mapper,
        StudentRepository studentRepository)
    {
        var createStudent = _studentFixture.CreateStudent;
        var studentDocument = _studentFixture.StudentDocument;
        var mockMongoCollection = mongoService.Database.GetCollection<StudentDocument>("student");

        mapper.Map<StudentDocument>(Arg.Any<CreateStudent>()).Returns(studentDocument);
        await mockMongoCollection.InsertOneAsync(Arg.Any<StudentDocument>(), Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>());

        var result = await studentRepository.CreateAsync(createStudent);

        await mockMongoCollection.Received().InsertOneAsync(Arg.Any<StudentDocument>(), Arg.Any<InsertOneOptions>(),
            Arg.Any<CancellationToken>());
        result.Should().Be(studentDocument);
    }

    [Theory]
    [AutoMoqData]
    public async Task DeleteByIdAsync_Returns_Acknowledged_True([Frozen] IMongoDbService mongoService,
        StudentRepository studentRepository)
    {
        var id = _fixture.Create<string>();
        var acknowledged = _fixture.Create<DeleteResult.Acknowledged>();
        var mockMongoCollection = mongoService.Database.GetCollection<StudentDocument>("student");

        mockMongoCollection.DeleteOneAsync(Arg.Any<FilterDefinition<StudentDocument>>(), Arg.Any<CancellationToken>())
            .Returns(acknowledged);

        var result = await studentRepository.DeleteByIdAsync(id);

        await mockMongoCollection.Received(1)
            .DeleteOneAsync(Arg.Any<FilterDefinition<StudentDocument>>(), Arg.Any<CancellationToken>());
        result.Should().Be(true);
    }

    [Theory]
    [AutoMoqData]
    public async Task GetByIdAsync_Returns_Student([Frozen] IMongoDbService mongoService,
        StudentRepository studentRepository)
    {
        var id = _fixture.Create<string>();
        var studentDocument = _studentFixture.StudentDocument;
        var mockMongoCollection = mongoService.Database.GetCollection<StudentDocument>("student");
        var mockCursor = Substitute.For<IAsyncCursor<StudentDocument>>();

        mockMongoCollection.FindAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<FindOptions<StudentDocument>>(),
            Arg.Any<CancellationToken>()).Returns(mockCursor);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true);
        mockCursor.Current.Returns(new List<StudentDocument> { studentDocument });

        var result = await studentRepository.GetByIdAsync(id);

        await mockMongoCollection.Received(1).FindAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<FindOptions<StudentDocument>>(),
            Arg.Any<CancellationToken>());

        result.Should().Be(studentDocument);
    }

    [Theory]
    [AutoMoqData]
    public async Task UpdateAsync_Acknowledged_False_Returns_Student_Null([Frozen] IMongoDbService mongoService,
        StudentRepository studentRepository)
    {
        var unacknowledged = _fixture.Create<ReplaceOneResult.Unacknowledged>();
        var updateStudent = _studentFixture.UpdateStudent;
        var studentDocument = _studentFixture.StudentDocument;
        var mockMongoCollection = mongoService.Database.GetCollection<StudentDocument>("student");
        var mockCursor = Substitute.For<IAsyncCursor<StudentDocument>>();

        mockMongoCollection.FindAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<FindOptions<StudentDocument>>(),
            Arg.Any<CancellationToken>()).Returns(mockCursor);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true);
        mockCursor.Current.Returns(new List<StudentDocument> { studentDocument });

        mockMongoCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<StudentDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>()).Returns(unacknowledged);

        var result = await studentRepository.UpdateAsync(updateStudent);

        await mockMongoCollection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<StudentDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());
        result.Should().Be(null);
    }

    [Theory]
    [AutoMoqData]
    public async Task UpdateAsync_Acknowledged_True_Returns_Student([Frozen] IMongoDbService mongoService,
        [Frozen] IMapper mapper, StudentRepository studentRepository)
    {
        var updateStudent = _studentFixture.UpdateStudent;
        var studentDocument = _studentFixture.StudentDocument;
        var mappedStudent = _studentFixture.MappedStudent(studentDocument, updateStudent);
        var mockMongoCollection = mongoService.Database.GetCollection<StudentDocument>("student");
        var mockCursor = Substitute.For<IAsyncCursor<StudentDocument>>();

        mockMongoCollection.FindAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<FindOptions<StudentDocument>>(),
            Arg.Any<CancellationToken>()).Returns(mockCursor);
        mockCursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true);
        mockCursor.Current.Returns(new List<StudentDocument> { studentDocument });

        mockMongoCollection.ReplaceOneAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<StudentDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>()).Returns(new ReplaceOneResult.Acknowledged(1, 1, studentDocument.Id));

        mapper.Map<StudentDocument>(Arg.Any<UpdateStudent>()).Returns(mappedStudent);

        var result = await studentRepository.UpdateAsync(updateStudent);

        await mockMongoCollection.Received(1).ReplaceOneAsync(
            Arg.Any<FilterDefinition<StudentDocument>>(),
            Arg.Any<StudentDocument>(),
            Arg.Any<ReplaceOptions>(),
            Arg.Any<CancellationToken>());

        mapper.Received(1).Map<StudentDocument>(Arg.Any<UpdateStudent>());

        result.Should().Be(mappedStudent);
    }
}
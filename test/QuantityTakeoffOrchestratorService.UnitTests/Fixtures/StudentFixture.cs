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
using Bogus;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Documents;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Requests;
using QuantityTakeoffOrchestratorService.Controllers.Students.Models.Responses;

namespace QuantityTakeoffOrchestratorService.UnitTests.Fixtures;

public class StudentFixture
{
    private readonly Fixture _fixture;

    /// <summary>
    ///     Constructor
    /// </summary>
    public StudentFixture()
    {
        var faker = new Faker();
        _fixture = new Fixture();

        _fixture.Customize<CreateStudent>(composer => composer
            .OmitAutoProperties()
            .Do(student =>
            {
                student.GivenName = faker.Name.FirstName();
                student.FamilyName = faker.Name.LastName();
                student.Email = faker.Internet.Email(student.GivenName, student.FamilyName);
            }));

        _fixture.Customize<StudentDocument>(composer => composer
            .OmitAutoProperties()
            .Do(student =>
            {
                student.Id = Guid.NewGuid().ToString();
                student.GivenName = faker.Name.FirstName();
                student.FamilyName = faker.Name.LastName();
                student.Email = faker.Internet.Email(student.GivenName, student.FamilyName);
                student.Modified = DateTime.UtcNow;
                student.Created = faker.Date.Past();
            }));

        _fixture.Customize<Student>(composer => composer
            .OmitAutoProperties()
            .Do(student =>
            {
                student.Id = Guid.NewGuid().ToString();
                student.GivenName = faker.Name.FirstName();
                student.FamilyName = faker.Name.LastName();
                student.Email = faker.Internet.Email(student.GivenName, student.FamilyName);
                student.Modified = DateTime.UtcNow;
                student.Created = faker.Date.Past();
            }));

        _fixture.Customize<UpdateStudent>(composer => composer
            .OmitAutoProperties()
            .Do(updateStudent =>
            {
                updateStudent.Id = Guid.NewGuid().ToString();
                updateStudent.GivenName = faker.Name.FirstName();
                updateStudent.FamilyName = faker.Name.LastName();
                updateStudent.Email = faker.Internet.Email(updateStudent.GivenName, updateStudent.FamilyName);
            }));
    }

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="CreateStudent" />.
    /// </summary>
    public CreateStudent CreateStudent => _fixture.Create<CreateStudent>();

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="StudentDocument" />.
    /// </summary>
    public StudentDocument StudentDocument => _fixture.Create<StudentDocument>();

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="Student" />.
    /// </summary>
    public Student Student => _fixture.Create<Student>();

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="IEnumerable{T}" />.
    /// </summary>
    public IEnumerable<Student> Students => _fixture.Create<IEnumerable<Student>>();

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="IEnumerable{T}" />.
    /// </summary>
    public IEnumerable<StudentDocument> StudentDocuments => _fixture.CreateMany<StudentDocument>(7);

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="UpdateStudent" />.
    /// </summary>
    public UpdateStudent UpdateStudent => _fixture.Create<UpdateStudent>();

    /// <summary>
    ///     Gets studentDocument after the mapping with UpdatedStudent <see cref="UpdateStudent" />.
    /// </summary>
    public StudentDocument MappedStudent(StudentDocument studentDocument, UpdateStudent updateStudent) =>
        _fixture.Build<StudentDocument>()
            .With(x => x.FamilyName, updateStudent.FamilyName)
            .With(x => x.GivenName, updateStudent.GivenName)
            .With(x => x.Id, updateStudent.Id)
            .With(x => x.Email, updateStudent.Email)
            .With(x => x.Created, studentDocument.Created)
            .With(x => x.Modified, studentDocument.Modified).Create();

    /// <summary>
    ///     Gets student after the mapping with <see cref="StudentDocument" />.
    /// </summary>
    public Student MappedStudent(Student student, StudentDocument studentDocument) =>
        _fixture.Build<Student>()
            .With(x => x.FamilyName, studentDocument.FamilyName)
            .With(x => x.GivenName, studentDocument.GivenName)
            .With(x => x.Id, studentDocument.Id)
            .With(x => x.Email, studentDocument.Email)
            .With(x => x.Created, studentDocument.Created)
            .With(x => x.Modified, studentDocument.Modified).Create();
}

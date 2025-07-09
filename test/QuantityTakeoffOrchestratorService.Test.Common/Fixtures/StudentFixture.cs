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
using QuantityTakeoffOrchestratorService.Test.Common.Models;
using QuantityTakeoffOrchestratorService.Test.Common.Models.Requests;

namespace QuantityTakeoffOrchestratorService.Test.Common.Fixtures;

/// <summary>
///     A fixture for all operations pertaining to the student controller.
/// </summary>
public class StudentFixture
{
    public const string PseudoProvider = "pseudo-email.com";
    private readonly Fixture _fixture;

    /// <summary>
    ///     Constructor
    /// </summary>
    public StudentFixture()
    {
        var faker = new Faker();
        _fixture = new Fixture();

        _fixture.Customize<StudentPoco>(composer => composer
            .OmitAutoProperties()
            .Do(student =>
            {
                student.Id = Guid.NewGuid().ToString();
                student.GivenName = faker.Name.FirstName();
                student.FamilyName = faker.Name.LastName();
                student.Email = faker.Internet.Email(student.GivenName, student.FamilyName, PseudoProvider);
                student.Modified = DateTime.UtcNow;
                student.Created = faker.Date.Past();
            }));

        _fixture.Customize<CreateStudentPoco>(composer => composer
            .OmitAutoProperties()
            .Do(createStudent =>
            {
                createStudent.GivenName = faker.Name.FirstName();
                createStudent.FamilyName = faker.Name.LastName();
                createStudent.Email = faker.Internet.Email(createStudent.GivenName, createStudent.FamilyName, PseudoProvider);
            }));

        _fixture.Customize<UpdateStudentPoco>(composer => composer
            .OmitAutoProperties()
            .Do(updateStudent =>
            {
                updateStudent.Id = Guid.NewGuid().ToString();
                updateStudent.GivenName = faker.Name.FirstName();
                updateStudent.FamilyName = faker.Name.LastName();
                updateStudent.Email = faker.Internet.Email(updateStudent.GivenName, updateStudent.FamilyName, PseudoProvider);
            }));
    }

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="UpdateStudentPoco" />.
    /// </summary>
    public UpdateStudentPoco UpdateStudentPoco => _fixture.Create<UpdateStudentPoco>();

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="StudentPoco" />.
    /// </summary>
    public StudentPoco StudentPoco => _fixture.Create<StudentPoco>();

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="IEnumerable{T}" />.
    /// </summary>
    public IEnumerable<StudentPoco> Students => _fixture.CreateMany<StudentPoco>(7);

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="IEnumerable{T}" />.
    /// </summary>
    public IEnumerable<CreateStudentPoco> CreateStudents => _fixture.CreateMany<CreateStudentPoco>(7);

    /// <summary>
    ///     Gets a new auto generated and randomized <see cref="CreateStudentPoco" /> request object.
    /// </summary>
    public CreateStudentPoco CreateStudentPoco => _fixture.Create<CreateStudentPoco>();
}

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

using System.Diagnostics.CodeAnalysis;
using QuantityTakeoffOrchestratorService.Test.Common.Models.Requests;
using Xunit;

namespace QuantityTakeoffOrchestratorService.IntegrationTests.TestCases.StudentController;

/// <summary>
///     Contains the test cases for successful creation of students
/// </summary>
public static class CreateStudentSuccesses
{
    /// <summary>
    ///     The test cases for successful creation of students
    /// </summary>
    /// <returns>
    ///     returns <see cref="TheoryData{T}" /> with a string for the details of the test and a setup action to prepare
    ///     the test
    /// </returns>
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    public static TheoryData<string, Action<CreateStudentPoco>> TestCases = new()
    {
        { "Email is standard", x => x.Email = "username@domain.com" },
        { "Email username has hyphen", x => x.Email = "user-name@domain.com" },
        { "Email username has underscore", x => x.Email = "user_name@domain.com" },
        { "Email username has suffix using plus sign", x => x.Email = "user+suffix@domain.com" },
        { "Email domain has hyphen", x => x.Email = "user@email-domain.com" }
    };
}

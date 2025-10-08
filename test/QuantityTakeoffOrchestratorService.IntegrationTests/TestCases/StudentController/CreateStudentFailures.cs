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
using QuantityTakeoffOrchestratorService.Test.Common.Models;
using QuantityTakeoffOrchestratorService.Test.Common.Models.Requests;
using Mep.Platform.Extensions.DotNet;
using Mep.Platform.Extensions.FluentValidation.Errors;
using Xunit;

namespace QuantityTakeoffOrchestratorService.IntegrationTests.TestCases.StudentController;

/// <summary>
///     Contains the test cases for unsuccessful creation of students
/// </summary>
public static class CreateStudentFailures
{
    /// <summary>
    ///     The test cases for unsuccessful creation of students
    /// </summary>
    /// <returns>
    ///     returns <see cref="TheoryData{T}" /> with a string for the details of the test, a string that dictates the expected
    ///     error response message, and a setup action to prepare
    ///     the test
    /// </returns>
    [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible")]
    public static TheoryData<string, string, Action<CreateStudentPoco>> TestCases = new()
    {
        {
            "Email is empty",
            ErrorFormatter.GetRequired(nameof(StudentPoco.Email).SplitAtUpperCase().ToUpperStart()),
            x => x.Email = string.Empty
        },
        {
            "Email is null",
            ErrorFormatter.GetRequired(nameof(StudentPoco.Email).SplitAtUpperCase().ToUpperStart()),
            x => x.Email = null
        },
        {
            "Email is missing at symbol",
            ErrorFormatter.GetInvalidEmail(nameof(StudentPoco.Email).SplitAtUpperCase().ToUpperStart()),
            x => x.Email = "user-domain.com"
        },
        {
            "Email starts with at symbol",
            ErrorFormatter.GetInvalidEmail(nameof(StudentPoco.Email).SplitAtUpperCase().ToUpperStart()),
            x => x.Email = "@user@domain.com"
        },
        {
            "Email has two at symbols",
            ErrorFormatter.GetInvalidEmail(nameof(StudentPoco.Email).SplitAtUpperCase().ToUpperStart()),
            x => x.Email = "user@name@domain.com"
        },
        {
            "Given name is empty",
            ErrorFormatter.GetRequired(nameof(StudentPoco.GivenName).SplitAtUpperCase().ToUpperStart()),
            x => x.GivenName = string.Empty
        },
        {
            "Given name is null",
            ErrorFormatter.GetRequired(nameof(StudentPoco.GivenName).SplitAtUpperCase().ToUpperStart()),
            x => x.GivenName = null
        }
        // // uncomment these if you want to test the behavior of failed tests
        // {
        //     "Bad expected message",
        //     ErrorFormatter.GetRequired(nameof(StudentDocument.GivenName).SplitAtUpperCase().ToUpperStart()),
        //     x => x.Email = "user%email.com"
        // },
        // {
        //     "Manually thrown exception",
        //     ErrorFormatter.GetRequired(nameof(StudentDocument.GivenName).SplitAtUpperCase().ToUpperStart()),
        //     _ => throw new ArgumentException("This exception was manually thrown")
        // }
    };
}

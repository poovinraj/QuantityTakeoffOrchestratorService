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

namespace QuantityTakeoffOrchestratorService.Test.Common.Models;

/// <summary>
///     A record of a student.
/// </summary>
/// <remarks>
///     This class is a copy of one that is in the actual service. Consumers of this service will not have access to
///     request and response models. Integration tests should emulate the perspective of the consumer, so we recreate them
///     in the test project. This could help highlight consumer issues in the future regarding synchronicity of the data
///     types.
/// </remarks>
public class StudentPoco
{
    /// <summary>
    ///     The unique identifier of the student.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     The student's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    ///     The student's given (first) name.
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    ///     The student's family (last) name.
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    ///     When the record was created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    ///     When the record was last modified.
    /// </summary>
    public DateTime Modified { get; set; }
}

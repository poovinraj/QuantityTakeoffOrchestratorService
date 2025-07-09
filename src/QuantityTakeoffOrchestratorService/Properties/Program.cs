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

// Just exposing the program for tests to use.

using System.Diagnostics.CodeAnalysis;
// ReSharper disable CheckNamespace


/// <summary>
///     Expose the Program class for use with WebApplicationFactory
/// </summary>
[ExcludeFromCodeCoverage]
[SuppressMessage("Major", "S3903:Types should be defined in named namespaces")]
[SuppressMessage("Design", "CA1050:Declare types in namespaces")]
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}

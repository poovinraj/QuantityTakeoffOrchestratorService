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

using QuantityTakeoffOrchestratorService.Test.Common.Models;
using QuantityTakeoffOrchestratorService.Test.Common.Models.Requests;
using Flurl.Http;
using Mep.Platform.Extensions.Flurl;
using Mep.Platform.Extensions.Flurl.Models;

namespace QuantityTakeoffOrchestratorService.Test.Common.Clients;

/// <summary>
///     A client for making calls to the student controller
/// </summary>
public class StudentClient
{
    private const string ControllerName = "student";
    private readonly FlurlClient _flurlClient;

    public StudentClient(FlurlClient flurlClient) => _flurlClient = flurlClient.AllowAnyHttpStatus();

    /// <summary>
    ///     Create a student
    /// </summary>
    /// <param name="createStudentPoco"></param>
    /// <param name="accessToken"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public async Task<ClientJsonReply<StudentPoco?>> CreateAsync(CreateStudentPoco createStudentPoco,
        string? accessToken, string? version = null) => await _flurlClient.WithOAuthBearerToken(accessToken)
        .Request(version, ControllerName).PostJsonAsync(createStudentPoco).ReceiveReplyAsync<StudentPoco>();

    /// <summary>
    ///     Delete student by ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="accessToken"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public async Task<ClientReply> DeleteByIdAsync(string? id, string? accessToken, string? version = null) =>
        await _flurlClient.WithOAuthBearerToken(accessToken).Request(version, ControllerName, id).DeleteAsync()
            .ReceiveReplyAsync();

    /// <summary>
    ///     Get all students
    /// </summary>
    /// <param name="accessToken"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public async Task<ClientJsonReply<IEnumerable<StudentPoco>?>>
        GetAllAsync(string? accessToken, string? version = null) => await _flurlClient.WithOAuthBearerToken(accessToken)
        .Request(version, ControllerName, "all").GetAsync().ReceiveReplyAsync<IEnumerable<StudentPoco>>();

    /// <summary>
    ///     Get student by ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="accessToken"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public async Task<ClientJsonReply<StudentPoco?>>
        GetByIdAsync(string? id, string? accessToken, string? version = null) => await _flurlClient
        .WithOAuthBearerToken(accessToken).Request(version, ControllerName, id).GetAsync()
        .ReceiveReplyAsync<StudentPoco>();

    /// <summary>
    ///     Update a student
    /// </summary>
    /// <param name="updateStudentPoco"></param>
    /// <param name="accessToken"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public async Task<ClientJsonReply<StudentPoco?>> UpdateAsync(UpdateStudentPoco updateStudentPoco,
        string? accessToken, string? version = null) => await _flurlClient.WithOAuthBearerToken(accessToken)
        .Request(version, ControllerName).PutJsonAsync(updateStudentPoco).ReceiveReplyAsync<StudentPoco>();
}

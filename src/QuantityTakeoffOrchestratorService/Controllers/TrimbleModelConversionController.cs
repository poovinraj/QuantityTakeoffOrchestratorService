using Mep.Platform.Extensions.AspNetCore.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Asp.Versioning;

namespace QuantityTakeoffOrchestratorService.Controllers
{
    /// <summary>
    /// This sample controller illustrate how we can get customer-id associated with 
    /// current bearer token. This uses custom policy 
    /// ref ServiceCustomExtensions.Configure => authOptionsBuilder.AddCustomerPolicy
    /// </summary>
    [ApiVersion("1.0")]
    [ExcludeFromCodeCoverage]
    public class TrimbleModelConversionController : VersionedJsonApiController
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public TrimbleModelConversionController(IHttpContextAccessor httpContextAccessor) => this.httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// This method will return the customer id associated with given token (if valid)
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.TooManyRequests)]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("conversionstatus")]
        public async Task<ActionResult<string>> GetConversionStatus(string conversionId)
        {
            
            return Ok($"Conversion status for {conversionId} is pending. This is a placeholder response.");
        }

        [HttpGet("ping")]
        public async Task<ActionResult<string>> TestPing()
        {
            return Ok($"Ping successfull!");
        }
    }
}

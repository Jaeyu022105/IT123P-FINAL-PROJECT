using System;
using System.ServiceModel;
using System.Threading.Tasks;
using FoodLens.Api.DTOs;
using FoodLens.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodLens.Api.Controllers
{
    /// <summary>
    /// REST Proxy Controller for legacy SOAP Export.
    /// Exposes a REST endpoint which internally communicates with the self-hosted SOAP service.
    /// Satisfies Phase 5 leg of the architecture by keeping the MAUI client lightweight.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json", "application/xml")]
    public class ExportController : ControllerBase
    {
        /// <summary>
        /// POST /api/export
        /// Triggers the legacy SOAP service export for the given date range.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(DietLogExportXml), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> RequestExport([FromBody] ExportRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Create WCF Channel dynamically based on current host and scheme
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var soapEndpointAddress = $"{baseUrl}/soap/DietExportService.svc";

            var binding = new BasicHttpBinding();
            var endpoint = new EndpointAddress(soapEndpointAddress);
            var factory = new ChannelFactory<IDietExportService>(binding, endpoint);

            try
            {
                var channel = factory.CreateChannel();
                
                // Invoke WCF service method
                var result = await channel.ExportDietLog(requestDto.UserId, requestDto.FromDate, requestDto.ToDate);
                
                // Clean up channel
                ((IClientChannel)channel).Close();
                factory.Close();

                return Ok(result);
            }
            catch (FaultException<ExportFault> fe)
            {
                // Surface strongly-typed export fault (e.g. no logs in range)
                return BadRequest(new ErrorDto
                {
                    Error = new ErrorDetail { Code = "EXPORT_FAULT", Message = fe.Detail.Message }
                });
            }
            catch (FaultException fe)
            {
                // General SOAP/WCF fault
                return BadRequest(new ErrorDto
                {
                    Error = new ErrorDetail { Code = "SOAP_FAULT", Message = fe.Message }
                });
            }
            catch (Exception ex)
            {
                // Server communication error (e.g. timeout)
                return StatusCode(500, new ErrorDto
                {
                    Error = new ErrorDetail { Code = "SOAP_CONNECTION_ERROR", Message = $"Failed to communicate with SOAP service: {ex.Message}" }
                });
            }
            finally
            {
                if (factory.State == CommunicationState.Opened)
                {
                    factory.Abort();
                }
            }
        }
    }
}

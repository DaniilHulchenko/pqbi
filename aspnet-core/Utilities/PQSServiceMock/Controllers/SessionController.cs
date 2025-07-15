using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PQBI.Configuration;
using PQBI.Infrastructure;
using PQBI.Network.RestApi;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.PQZBinary;

//using PQS.Data.RecordsContainer;
//using PQS.Data.RecordsContainer.Records;
//using PQS.PQZBinary;
using PQSServiceMock.Model.Dtos;
using PQSServiceMock.Services;

namespace PQSServiceMock.Controllers
{
    [Route("[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ILogger<SessionController> _logger;
        private readonly IPQSenderHelper _pQSenderHelper;
        private readonly IPQSServiceAutoResponseManager _pQSServiceAutoResponsController;
        private readonly PQSComunication _config;

        // GET: SessionController
        public SessionController(
            ILogger<SessionController> logger,
            IPQSenderHelper pQSenderHelper,
            IOptions<PQSComunication> config,
            IPQSServiceAutoResponseManager pQSServiceAutoResponsController)
        {
            _logger = logger;
            _pQSenderHelper = pQSenderHelper;
            _pQSServiceAutoResponsController = pQSServiceAutoResponsController;
            _config = config.Value;

        }

        [HttpGet]
        [HttpGet("status")]
        public async Task<ActionResult> GetStatus()
        {
            return Ok("Service is working!!!");
        }


        [HttpPost]
        public async Task<SetSessionStateResponse> SetSessionStatusAsync([FromBody] SetSessionStatusRequest request)
        {
            var isChanged = await _pQSServiceAutoResponsController.ChangeTypeResponsesAsync(request.StateAlias);
            var response = new SetSessionStateResponse
            {
                SessionStatus = isChanged
            };

            return response;

        }



        [HttpPost("sendRequest/binary")]
        public async Task<IActionResult> ReturnBytes()
        {
            _logger.LogInformation($"xxx {nameof(ReturnBytes)} - Entered");
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                var stream = ms.ToArray();


                PQSRequest pqsRequest = PQZBinaryReader.ReadMessage(stream) as PQSRequest;
                List<PQSRecordBase> records = pqsRequest.GetRecords();
                byte[] buffer = null;

                foreach (PQSRecordBase rec in records)
                {
                    switch (rec.RecordType)
                    {
                        case RecordType.OperationRequest:
                            if (rec is OperationRequestRecord operationReq)
                            {
                                switch (operationReq.OperationType)
                                {
                                    case OperationType.OPEN_SESSION:

                                        buffer = await _pQSServiceAutoResponsController.OpenSession(stream);
                                        break;

                                    case OperationType.NOP:
                                        buffer = await _pQSServiceAutoResponsController.NopSession(stream);
                                        break;

                                    case OperationType.CLOSE_SESSION:
                                        buffer = await _pQSServiceAutoResponsController.CloseSession(stream);
                                        break;

                                    default:
                                        buffer = await _pQSServiceAutoResponsController.ProxyRequest(stream);
                                        break;
                                }
                            }
                            break;
                    }
                }

                Response.Headers["Content-Type"] = "application/octet-stream";
                Response.Headers["Content-Length"] = buffer.Length.ToString();
                Response.Headers["Content-Disposition"] = "attachment; filename=output.bin";
                Response.StatusCode = 200;

                return File(buffer, "application/octet-stream");
            }
        }
    }
}

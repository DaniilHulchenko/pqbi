//using Azure;
using Azure;
using Microsoft.Extensions.Logging;
using PQBI.Infrastructure.Extensions;
using PQBI.Network.Base;
using PQBI.Network.RestApi.Validations;
using PQBI.PQS;
using PQBI.Requests;
using PQS.Data.Common;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.PQZBinary;

namespace PQBI.Network.RestApi;

public abstract class PQSRestApiServiceBase : PQSServiceBase
{
    //protected const string PQS_COMMUNICATION_POST_URL = "binary";
    protected const string PQS_COMMUNICATION_POST_URL = "bin";


    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPQZBinaryWriterWrapper _pQZBinaryWriterCore;
    private readonly IPQSenderHelper _pQSenderHelper;

    public PQSRestApiServiceBase(IHttpClientFactory httpClientFactory, IPQZBinaryWriterWrapper pQZBinaryWriterCore, IPQSenderHelper pQSenderHelper, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _pQZBinaryWriterCore = pQZBinaryWriterCore;
        _pQSenderHelper = pQSenderHelper;
        Logger = logger;
    }

    protected ILogger Logger { get; }

    protected abstract string ClientAlias { get; }

    public async Task<string> PQSIndentifyAsync(string baseUrl)
    {

        var result = await _pQSenderHelper.PQSIndentifyAsync(baseUrl, ClientAlias);
        var response = result.ToString();

        Logger.LogInformation($"PQSIndentifyAsync  baseUrl = {baseUrl}  response = {response}");

        return response;
    }

    //protected async Task<PQSResponse> SendRecordsContainerPostRequest<TRequest>(string url, TRequest request) where TRequest : PQSRequestBase
    //{
    //    byte[] stream = _pQZBinaryWriterCore.WriteMessage(request);

    //    return await SendRecordsContainerPostRequest(url, stream);
    //}

    protected async Task<PQSResponse> SendRecordsContainerPostBinaryRequestAndException<TRequest>(string url, TRequest request) where TRequest : PQSRequest
    {
        byte[] stream = _pQZBinaryWriterCore.WriteMessage(request);
        url = UrlBinaryUrl(url);
        var result = await SendRecordsContainerPostRequest(url, stream);


        foreach (var record in result.GetRecords())
        {
            if (record is not null && record is ErrorRecord error)
            {
                if (error.Status == PQZStatus.SESSION_EXPIRED)
                {
                    Logger.LogError($"in Url ={url} PQZStatus = {error.Status.ToString()}");
                    throw new SessionExpiredException(result);
                }
                //else
                //{
                //    if (error.Status != PQZStatus.OK)
                //    {
                //        Logger.LogError($"in Url ={url} PQZStatus ={error.Status.ToString()}");
                //        throw new PQBIExceptionBase($"Response from scada is incorrect {error.ToString()}", result);
                //    }
                //}
            }
        }

        return result;
    }

    protected async Task<PQSResponse> SendRecordsContainerPostRequest(string url, byte[] stream)
    {
        var session = await _pQSenderHelper.SendAutoPostRequest(url, stream, ClientAlias);
        return session;
    }


    protected string UrlBinaryUrl(string url) => url.UrlCombine(PQS_COMMUNICATION_POST_URL);


    //protected async Task<PQSResponse> SendOperationPostRequest<TRequest>(string url, TRequest request) where TRequest : PQSRequestBase
    //{
    //    byte[] stream = _pQZBinaryWriterCore.WriteMessage(request);

    //    return await SendOperationPostRequest(url, stream);
    //}

    protected async Task<PQSResponse> SendOperationPostBinaryRequest<TRequest>(string url, TRequest request) where TRequest : PQSRequestBase
    {
        byte[] stream = _pQZBinaryWriterCore.WriteMessage(request);
        url = UrlBinaryUrl(url);
        return await SendOperationPostRequest(url, stream);
    }

    protected async Task<PQSResponse> SendOperationPostRequest(string url, byte[] stream)
    {
        var response = await _pQSenderHelper.SendAutoPostRequest(url, stream, ClientAlias);
        return response;
    }

}

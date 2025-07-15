using Microsoft.Extensions.Options;
using PQBI.Configuration;
using PQBI.Network.RestApi;
using PQS.Data.Common;
using PQS.Data.Configurations.Enums;
using PQS.Data.Configurations;
using PQS.Data.RecordsContainer;
using PQS.Data.RecordsContainer.Records;
using PQS.PQZBinary;
using PQBI.Infrastructure;

namespace PQSServiceMock.Services;

public interface IPQSServiceResponse
{
    Task<byte[]> CloseSession(byte[] request);
    Task<byte[]> OpenSession(byte[] request);
    Task<byte[]> NopSession(byte[] request);
    Task<byte[]> ProxyRequest(byte[] request);
}

public abstract class PQSServiceResponsesBase : IPQSServiceResponse
{
    protected readonly PQSComunication _config;
    protected IPQSenderHelper _pQSenderHelper;

    public PQSServiceResponsesBase(IOptions<PQSComunication> config, IPQSenderHelper pQSenderHelper)
    {
        _config = config.Value;
        _pQSenderHelper = pQSenderHelper;
    }

    public abstract string Alias { get; }

    public abstract Task<byte[]> OpenSession(byte[] request);

    public abstract Task<byte[]> NopSession(byte[] request);

    public abstract Task<byte[]> CloseSession(byte[] request);

    public async Task<byte[]> ProxyRequest(byte[] request)
    {
        var response = await _pQSenderHelper.SendStreamRequestBase64Resp(_config.PQSServiceRestUrl, request, IPQSRestApiService.Alias);
        return response;
    }
}

public class PQSUpResponses : PQSServiceResponsesBase
{
    public const string AliasIndentifier = "pqsup";

    public PQSUpResponses(IOptions<PQSComunication> config, IPQSenderHelper pQSenderHelper) : base(config, pQSenderHelper)
    {
        InitializeCreateSessionFakeResponse();
        InitializeNopFakeResponse();
        InitializeCloseSessionFakeResponse();
    }

    private void InitializeCloseSessionFakeResponse()
    {
        var fakeResponse = new PQSResponse();
        var status = PQZStatus.OK;
        var resultLicenseParams = new ConfigurationParameterAndValueContainer();
        var operationResponseRecord = new OperationResponseRecord(null, OperationType.CLOSE_SESSION, status, resultLicenseParams);
        fakeResponse.AddRecord(operationResponseRecord);
        CloseSessionSessionArray = PQZBinaryWriter.WriteMessage(fakeResponse);
    }

    private void InitializeCreateSessionFakeResponse()
    {
        var fakeResponse = new PQSResponse();
        var status = PQZStatus.OK;
        var resultLicenseParams = new ConfigurationParameterAndValueContainer();
        resultLicenseParams.AddParamWithValue<string>(StandardConfigurationEnum.STD_SESSION_ID, Guid.NewGuid().ToString());

        var operationResponseRecord = new OperationResponseRecord(null, OperationType.OPEN_SESSION, status, resultLicenseParams);

        fakeResponse.AddRecord(operationResponseRecord);
        OpenSessionSessionArray = PQZBinaryWriter.WriteMessage(fakeResponse);

    }

    private void InitializeNopFakeResponse()
    {
        var fakeResponse = new PQSResponse();
        var status = PQZStatus.OK;
        var resultLicenseParams = new ConfigurationParameterAndValueContainer();
        var operationResponseRecord = new OperationResponseRecord(null, OperationType.NOP, status, resultLicenseParams);

        fakeResponse.AddRecord(operationResponseRecord);
        NopSessionArray = PQZBinaryWriter.WriteMessage(fakeResponse);
    }

    public byte[] OpenSessionSessionArray { get; private set; }
    public byte[] NopSessionArray { get; private set; }
    public byte[] CloseSessionSessionArray { get; private set; }

    public override string Alias => AliasIndentifier;

    public async override Task<byte[]> OpenSession(byte[] request)
    {
        return OpenSessionSessionArray;
    }

    public override async Task<byte[]> NopSession(byte[] request)
    {
        return NopSessionArray;
    }

    public override async Task<byte[]> CloseSession(byte[] request)
    {
        return CloseSessionSessionArray;

    }
}

public class PQSDownResponses : PQSServiceResponsesBase
{
    public const string AliasIndentifier = "pqsdown";

    public PQSDownResponses(IOptions<PQSComunication> config, IPQSenderHelper pQSenderHelper) : base(config, pQSenderHelper)
    {
    }

    public override string Alias => AliasIndentifier;

    public override async Task<byte[]> CloseSession(byte[] request)
    {
        throw new NotImplementedException($"{nameof(PQSDownResponses)}-{nameof(PQSDownResponses.CloseSession)} exception");
    }

    public override async Task<byte[]> NopSession(byte[] request)
    {
        throw new NotImplementedException($"{nameof(PQSDownResponses)}-{nameof(PQSDownResponses.NopSession)} exception");
    }

    public async override Task<byte[]> OpenSession(byte[] request)
    {
        throw new TimeoutException($"{nameof(PQSDownResponses)}-{nameof(PQSDownResponses.OpenSession)} exception");
    }
}
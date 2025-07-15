using Microsoft.Extensions.Options;
using PQBI.Configuration;
using PQBI.Infrastructure;
using PQBI.Network.RestApi;

namespace PQSServiceMock.Services;

public interface IPQSServiceAutoResponseManager : IPQSServiceResponse
{
    Task<bool> ChangeTypeResponsesAsync(string alias);
}

public class PQSServiceAutoResponseManager : IPQSServiceAutoResponseManager
{
    public PQSServiceAutoResponseManager(IOptions<PQSComunication> config, IPQSenderHelper pQSenderHelper)
    {
        PQSDown = new PQSDownResponses(config, pQSenderHelper);
        CurrentResponses = PQSUP= new PQSUpResponses(config, pQSenderHelper);
    }

    public IPQSServiceResponse PQSUP { get; }
    public IPQSServiceResponse CurrentResponses { get; private set; }
    public IPQSServiceResponse PQSDown { get; }

    public async Task<byte[]> CloseSession(byte[] request)
    {
        return await CurrentResponses.CloseSession(request);
    }

    public async Task<byte[]> OpenSession(byte[] request)
    {
        return await CurrentResponses.OpenSession(request);
    }

    public async Task<byte[]> NopSession(byte[] request)
    {
        return await CurrentResponses.NopSession(request);
    }

    public async Task<byte[]> ProxyRequest(byte[] request)
    {
        return await CurrentResponses.ProxyRequest(request);

    }

    public async Task<bool> ChangeTypeResponsesAsync(string alias)
    {
        var flag = false;
        if (PQSUpResponses.AliasIndentifier.Equals(alias, StringComparison.OrdinalIgnoreCase))
        {
            CurrentResponses = PQSUP;
            flag = true;
        }
        else
        {
            if (PQSDownResponses.AliasIndentifier.Equals(alias, StringComparison.OrdinalIgnoreCase))
            {
                CurrentResponses = PQSDown;
                flag = true;
            }
        }

        return flag;
    }
}
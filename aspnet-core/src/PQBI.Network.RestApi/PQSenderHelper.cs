using Microsoft.Extensions.Logging;
using Polly.Timeout;
using PQBI.Infrastructure.Extensions;
using PQS.Data.RecordsContainer;
using PQS.PQZBinary;

namespace PQBI.Network.RestApi;


public interface IPQSenderHelper
{
    Task<string> PQSIndentifyAsync(string baseUrl, string configName);
    Task<PQSResponse> SendAutoPostRequest(string url, byte[] stream, string configName);
    //Task<PQSResponse> SendStreamRequestAutoResponse(string url, byte[] stream, string configName);
    Task<byte[]> SendStreamRequestBase64Resp(string url, byte[] stream, string configName);
}

public class PQSenderHelper : IPQSenderHelper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PQSenderHelper> _logger;

    public PQSenderHelper(IHttpClientFactory httpClientFactory, ILogger<PQSenderHelper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> PQSIndentifyAsync(string baseUrl, string configName)
    {
        var result = string.Empty;
        //var uri = $"{baseUrl}identify";
        var uri = baseUrl.UrlCombine("identify");

        using (var httpClient = _httpClientFactory.CreateClient(configName))
        using (var postRes = await httpClient.GetAsync(uri))
        {
            if (postRes.IsSuccessStatusCode)
            {
                result = await postRes.Content.ReadAsStringAsync();
            }
        }

        return result;
    }

    public async Task<PQSResponse> SendAutoPostRequest(string url, byte[] stream, string configName)
    {
        PQSRecordsContainer result = null;
        //byte[] bytes;

        /* Testing env: local (launchSettings.json) or docker - mock goes for original binary response
           Staiging env: real PQSCADA uses base64 response from v1.0.8.0 
        */
        //if (url.Contains("pqbi.mock") || url.Contains("localhost:7017") || url.Contains("localhost:5128"))
        //    bytes = await SendStreamRequest(url, stream, configName);
        //else
        var bytes = await SendStreamRequestBase64Resp(url, stream, configName);

        if (bytes != null)
        {
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                result = PQZBinaryReader.ReadMessage(memoryStream);
            }
        }

        var tmp = result as PQSResponse;
        return tmp;
    }

    public async Task<byte[]> SendStreamRequestBase64Resp(string url, byte[] stream, string configName)
    {
        byte[] mainResult = null;
        var byteArrayContent = new ByteArrayContent(stream);

        byteArrayContent.Headers.Add("Content-Type", "application/octet-stream");
        var timeout = TimeSpan.Zero;

        try
        {
            using (var httpClient = _httpClientFactory.CreateClient(configName))
            {
                timeout = httpClient.Timeout;
                using (var postRes = await httpClient.PostAsync(url, byteArrayContent))
                {
                    if (postRes.IsSuccessStatusCode)
                    {
                        //Fix
                        mainResult = await postRes.Content.ReadAsByteArrayAsync();


                        //var result = await postRes.Content.ReadAsStringAsync();
                        //if (result.StartsWith("\""))
                        //    result = result.Substring(1);

                        //if (result.EndsWith("\""))
                        //    result = result.Substring(0, result.Length - 1);
                        //mainResult = Convert.FromBase64String(result);
                    }
                }
            }
        }
        catch (TimeoutRejectedException ex) // Polly Timeout Exception
        {
            _logger.LogError(ex, "Timeout from SCADA to {Url} timed out after {Timeout} seconds.", url, timeout.TotalSeconds);
            throw;
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested) // Handles HTTP client timeout
        {
            _logger.LogError(ex, "Request to {Url} timed out or was canceled.", url);
            throw;
        }

        //catch (Exception ex)
        //{
        //    throw;
        //}

        return mainResult;
    }


}
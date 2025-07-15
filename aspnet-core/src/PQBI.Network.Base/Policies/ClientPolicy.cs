using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using PQBI.Infrastructure;
using PQBI.Policies;

namespace PQBI.Network.Base.Policies;

public class ClientPolicyConfig : PQSConfig<ClientPolicyConfig>
{
    public int RetryAmount { get; set; }
}



public class ClientPolicy
{
    private readonly ILogger<ClientPolicy> _logger;
    //private readonly ClientPolicyConfig _config;

    public IAsyncPolicy<HttpResponseMessage> PolicyWrap { get; }

    public ClientPolicy(ILogger<ClientPolicy> logger)
    {
        _logger = logger;

        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(300));

        var retryOnTimeoutPolicy = Policy
            .Handle<TimeoutRejectedException>() // Retry only on timeout exception
            .WaitAndRetryAsync(1, _ => TimeSpan.FromSeconds(2), (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogWarning("Timeout occurred. Retrying in {Delay}s... Attempt {RetryCount}", timeSpan.TotalSeconds, retryCount);
            });

        PolicyWrap = retryOnTimeoutPolicy.WrapAsync(timeoutPolicy);

        //PolicyWrap = Policy.WrapAsync(timeoutPolicy, retryOnTimeoutPolicy);
    }
}


//public class ClientPolicy
//{
//    private readonly ILogger<ClientPolicy> _logger;
//    private readonly ClientPolicyConfig _config;

//    public AsyncRetryPolicy<HttpResponseMessage> ImediateHttpRetry { get; }


//    public ClientPolicy(ILogger<ClientPolicy> logger, IOptions<ClientPolicyConfig> config)
//    {
//        _logger = logger;
//        _config = config.Value;

//        ImediateHttpRetry = Policy.HandleResult<HttpResponseMessage>(res =>
//        {
//            if (!res.IsSuccessStatusCode)
//            {
//                _logger.LogCommunication(res);
//            }

//            return !res.IsSuccessStatusCode;

//        }).WaitAndRetryAsync(_config.RetryAmount, retryAttempt =>
//        {
//            return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
//        });
//    }
//}

using GrpcService1;
using Microsoft.Extensions.Options;
using Grpc.Net.Client;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core.Interceptors;
using Grpc.Core;
using Abp.UI;
using PQBI.Configuration;
using PQBI.Network.Base;
using PQBI.Infrastructure;

namespace PQBI.Network.Grpc
{
    public class PQSGrpcServiceBase : PQSServiceBase
    {
        private readonly PQSCommunication.PQSCommunicationClient _pQSCommunicationClient;
        protected PQSComunication PQsCommunicationConfig;

        protected PQSGrpcServiceBase(IOptions<PQSComunication> pQsCommunicationConfig, PQSCommunication.PQSCommunicationClient pQSCommunicationClient)
        {
            PQsCommunicationConfig = pQsCommunicationConfig.Value;
            _pQSCommunicationClient = pQSCommunicationClient;
        }

        public async Task<string> IndentifyAsync(RequestIdentify request, string url)
        {
            try
            {
                var client = _pQSCommunicationClient;
                var grpcResponse = await client.IdentifyRequestXMLAsync(request);

                if (string.IsNullOrEmpty(grpcResponse.Message))
                {
                    throw new UserFriendlyException("Indentify failed");
                }

                return grpcResponse.Message;
            }
            catch (UserFriendlyException e)
            {
                //TODO: Should be implemented in PQBI-14.
                throw new UserFriendlyException($"No connection was established - {e.Message}");
            }
        }

        public async Task<string> IndentifyAsync(string url)
        {
            try
            {
                var client = _pQSCommunicationClient;
                var request = new RequestIdentify();
                var grpcResponse = await client.IdentifyRequestXMLAsync(request);

                if (string.IsNullOrEmpty(grpcResponse.Message))
                {
                    //TODO: Should be implemented in PQBI-14.
                    throw new UserFriendlyException("Indentify failed");
                }

                return grpcResponse.Message;
            }
            catch (UserFriendlyException e)
            {
                //TODO: Should be implemented in PQBI-14.
                throw new UserFriendlyException($"No connection was established - {e.Message}");
            }
        }


        public async Task<string> RequestXmlAsync(string url, string message)
        {
            try
            {
                var request = new RequestString() { Message = message };
                var client = _pQSCommunicationClient;
                var responseString = await client.RequestXMLAsync(request);
                if (string.IsNullOrEmpty(responseString.Message))
                {
                    //TODO: Should be implemented in PQBI-14.
                    throw new UserFriendlyException($"{nameof(RequestXmlAsync)} failed");
                }

                return responseString.Message;
            }
            catch (UserFriendlyException e)
            {
                //TODO: Should be implemented in PQBI-14.
                throw new UserFriendlyException(e.Message);
            }
        }


        private PQSCommunication.PQSCommunicationClient CreateGrpcClient(string url)
        {
            System.Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> sslCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            if (PQsCommunicationConfig.IsAllCertificatesTrusted == false)
            {
                sslCallback = (HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslErrors) =>
                {
                    var result = sslErrors == SslPolicyErrors.None;
                    return result;
                };
            }

            var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = sslCallback } });
            var invoker = channel.Intercept(new ClientLoggingInterceptor());
            var client = new PQSCommunication.PQSCommunicationClient(invoker);


            return client;
        }
    }

    public class ClientLoggingInterceptor : Interceptor
    {
        public ClientLoggingInterceptor()
        {
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            return continuation(request, context);
        }
    }
}
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.AspNetCore.FluentValidation.SampleRpc;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Grpc.AspNetCore.FluentValidation.Test
{
    public static class WebApplicationFactoryHelper
    {
        public static GrpcChannel CreateGrpcChannel(this WebApplicationFactory<Startup> factory)
        {
            return CreateGrpcChannel(factory, new ResponseVersionHandler());
        }
        
        public static GrpcChannel CreateGrpcChannel(this WebApplicationFactory<Startup> factory, DelegatingHandler handler)
        {
            var client = factory.CreateDefaultClient(handler);
            return GrpcChannel.ForAddress(client.BaseAddress, new GrpcChannelOptions
            {
                HttpClient = client
            });
        }

    }
    
    internal class ResponseVersionHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            response.Version = request.Version;

            return response;
        }
    }
}
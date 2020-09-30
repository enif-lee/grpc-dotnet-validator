using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Grpc.AspNetCore.FluentValidation.SampleRpc;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Grpc.AspNetCore.FluentValidation.Test.Integration
{
    public class CustomValidatorIntegrationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        public CustomValidatorIntegrationTest(WebApplicationFactory<Startup> factory)
        {
            _factory = factory
                .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                {
                    services.AddValidator<HelloRequestValidator>();
                }));
        }

        private readonly WebApplicationFactory<Startup> _factory;

        [Fact]
        public async Task Should_ResponseMessage_When_MessageIsValid()
        {
            // Given
            var client = new Greeter.GreeterClient(_factory.CreateGrpcChannel());

            // When
            await client.SayHelloAsync(new HelloRequest
            {
                Name = "Not Empty Name"
            });

            // Then nothing happen.
        }

        [Fact]
        public async Task Should_ThrowInvalidArgument_When_NameOfMessageIsEmpty()
        {
            // Given
            var client = new Greeter.GreeterClient(_factory.CreateGrpcChannel());

            // When
            async Task Action()
            {
                await client.SayHelloAsync(new HelloRequest {Name = string.Empty});
            }

            // Then
            var rpcException = await Assert.ThrowsAsync<RpcException>(Action);
            Assert.Equal(StatusCode.InvalidArgument, rpcException.Status.StatusCode);
        }
        
        [Fact]
        public async Task Should_ReturnWithTrailingHeader_When_RequestIsInvalid()
        {
            // Given
            var spyHandler = new VerifierHeaderSpyDelegate();
            var client = new Greeter.GreeterClient(_factory.CreateGrpcChannel(spyHandler));

            // When
            await client.SayHelloAsync(new HelloRequest {Name = string.Empty}).ResponseHeadersAsync;

            // Then
            var headers = spyHandler.ResponseMessage.Headers;
            headers.TryGetValues("grpc-status", out var values);
            Assert.Single(values, "3");
            Assert.True(headers.Contains("grpc-message"));
        }

        class VerifierHeaderSpyDelegate : ResponseVersionHandler
        {
            public HttpResponseMessage ResponseMessage { get; set; }
            
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                ResponseMessage = await base.SendAsync(request, cancellationToken);
                return ResponseMessage;
            }
        }
        
        public class HelloRequestValidator : AbstractValidator<HelloRequest>
        {
            public HelloRequestValidator()
            {
                RuleFor(request => request.Name).NotEmpty();
            }
        }
    }
}
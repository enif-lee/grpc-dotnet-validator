using System.Threading.Tasks;
using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Grpc.AspNetCore.FluentValidation.Internal
{
    internal class ValidationInterceptor : Interceptor
    {
        private readonly IValidatorLocator _locator;
        private readonly IValidatorErrorMessageHandler _handler;

        public ValidationInterceptor(IValidatorLocator locator, IValidatorErrorMessageHandler handler)
        {
            _locator = locator;
            _handler = handler;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            await CheckRequestMessageAsync(request);
            return await continuation(request, context);
        }

        public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            await CheckRequestStreamAsync(requestStream);
            return await continuation(requestStream, context);
        }

        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            await CheckRequestMessageAsync(request);
            await continuation(request, responseStream, context);
        }

        public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream,
            IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
        {
            await CheckRequestStreamAsync(requestStream);
            await continuation(requestStream, responseStream, context);
        }

        private async Task CheckRequestMessageAsync<TRequest>(TRequest request) where TRequest : class
        {
            if (_locator.TryGetValidator<TRequest>(out var validator))
                await ValidateAsync(request, validator);
        }

        private async Task CheckRequestStreamAsync<TRequest>(IAsyncStreamReader<TRequest> requestStream) where TRequest : class
        {
            if (_locator.TryGetValidator<TRequest>(out var validator))
            {
                do
                {
                    await ValidateAsync(requestStream.Current, validator);
                } while (await requestStream.MoveNext());
            }
        }

        private async Task ValidateAsync<TRequest>(TRequest request, IValidator<TRequest> validator)
        {
            var results = await validator.ValidateAsync(request);
            if (!results.IsValid)
            {
                var message = await _handler.HandleAsync(results.Errors);
                throw new RpcException(new Status(StatusCode.InvalidArgument, message));
            }
        }
    }
}
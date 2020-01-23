using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Grpc.AspNetCore.FluentValidation.Internal
{
    internal class ValidationInterceptor : Interceptor
    {
        private readonly IValidatorLocator _locator;
        private readonly IValidatorErrorMessageHandler _handler;
        private const string StatusTrailerName = "grpc-status";
        private const string StatusTrailerInvalidArgument = "3";

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
            if (_locator.TryGetValidator<TRequest>(out var validator))
            {
                var results = await validator.ValidateAsync(request);
                if (!results.IsValid)
                {
                    var message = await _handler.HandleAsync(results.Errors);
                    context.Status = new Status(StatusCode.InvalidArgument, message);
                    context.GetHttpContext().Response.Headers[StatusTrailerName] = StatusTrailerInvalidArgument;
                    return ObjectCreator<TResponse>.Empty;
                }
            }

            return await continuation(request, context);
        }
    }
}
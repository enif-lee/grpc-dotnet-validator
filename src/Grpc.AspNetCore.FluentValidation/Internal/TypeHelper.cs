using System;
using System.Linq;
using FluentValidation;

namespace Grpc.AspNetCore.FluentValidation.Internal
{
    internal static class TypeHelper
    {
        public static Type GetServiceTypeFromValidatorTYpe<TService>() where TService : class, IValidator
        {
            var type = typeof(TService);
            var validatorType =  type.GetInterfaces()
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IValidator<>));

            if (validatorType == null)
                throw new AggregateException(type.Name + "is not implement with IValidator<>.");

            var messageType = validatorType.GetGenericArguments().First();
            var serviceType = typeof(IValidator<>).MakeGenericType(messageType);

            return serviceType;
        }
    }
}
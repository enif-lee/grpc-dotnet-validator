using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Grpc.AspNetCore.FluentValidation
{
    /// <summary>
    ///     Grpc validator profile for categorizing request validators
    /// </summary>
    public abstract class ValidatorProfileBase
    {
        internal IServiceCollection Validators { get; } = new ServiceCollection();

        /// <summary>
        ///     Add inline request validator
        /// </summary>
        /// <typeparam name="TRequest">GRPC request type</typeparam>
        /// <returns></returns>
        protected AbstractValidator<TRequest> CreateInlineValidator<TRequest>()
        {
            var validator = new InlineValidator<TRequest>();
            Validators.AddSingleton<IValidator<TRequest>>(validator);
            return validator;
        }
        
        /// <summary>
        ///     Add validator type
        /// </summary>
        /// <typeparam name="TValidator"></typeparam>
        protected void AddValidator<TValidator>() where TValidator : class, IValidator
        {
            AddValidator<TValidator>(ServiceLifetime.Scoped);
        }

        /// <summary>
        ///     Add validator type
        /// </summary>
        /// <param name="lifetime"></param>
        /// <typeparam name="TValidator"></typeparam>
        protected void AddValidator<TValidator>(ServiceLifetime lifetime) where TValidator : class, IValidator
        {
            Validators.AddValidator<TValidator>(lifetime);
        }
    }
}
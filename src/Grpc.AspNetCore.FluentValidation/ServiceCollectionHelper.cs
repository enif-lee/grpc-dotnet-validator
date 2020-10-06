using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentValidation;
using Grpc.AspNetCore.FluentValidation.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grpc.AspNetCore.FluentValidation
{
    public static class ServiceCollectionHelper
    {
        private static void AddGrpcValidatorCore(IServiceCollection services)
        {
            services.TryAddScoped<IValidatorLocator>(provider => new ServiceCollectionValidationProvider(provider));
            services.TryAddSingleton<IValidatorErrorMessageHandler, DefaultErrorMessageHandler>();
        }

        /// <summary>
        ///     Add custom message validator.
        /// </summary>
        /// <param name="services">service collection</param>
        /// <param name="lifetime">specific life time for validator</param>
        /// <typeparam name="TValidator">custom validator type</typeparam>
        /// <returns></returns>
        /// <exception cref="AggregateException">When try to register along validator class.</exception>
        public static IServiceCollection AddValidator<TValidator>(this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Scoped) where TValidator : class, IValidator
        {
            AddGrpcValidatorCore(services);
            var serviceType = TypeHelper.GetServiceTypeFromValidatorTYpe<TValidator>();
            services.TryAdd(new ServiceDescriptor(serviceType, typeof(TValidator), lifetime));
            return services;
        }

        /// <summary>
        ///     Add inline validator for simple rule.
        /// </summary>
        /// <param name="services">service collection</param>
        /// <param name="validator">configure validation rules</param>
        /// <typeparam name="TMessage">grpc message type</typeparam>
        /// <returns></returns>
        public static IServiceCollection AddInlineValidator<TMessage>(this IServiceCollection services, 
            Action<AbstractValidator<TMessage>> validator)
        {
            AddGrpcValidatorCore(services);
            services.AddSingleton<IValidator<TMessage>>(new InlineValidator<TMessage>(validator));
            return services;
        }

        /// <summary>
        ///     Add validator profile
        /// </summary>
        /// <param name="services"></param>
        /// <param name="profile">profile instance</param>
        /// <returns></returns>
        public static IServiceCollection AddValidatorProfile(this IServiceCollection services, ValidatorProfileBase profile)
        {
            AddGrpcValidatorCore(services);
            services.Add(profile.Validators);
            return services;
        }

        /// <summary>
        ///     Add validator profile
        /// </summary>
        /// <param name="services"></param>
        /// <typeparam name="TProfile">validator profile type</typeparam>
        /// <returns></returns>
        public static IServiceCollection AddValidatorProfile<TProfile>(this IServiceCollection services)
            where TProfile : ValidatorProfileBase, new()
        {
            services.AddValidatorProfile(new TProfile());
            return services;
        }

        /// <summary>
        /// Adds all validators from calling assembly 
        /// </summary>
        /// <param name="services">The collection of services</param>
        /// <param name="lifetime">The lifetime of the validators. The default is transient</param>
        /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddValidatorsFromAssemblies(this IServiceCollection services, 
            ServiceLifetime lifetime = ServiceLifetime.Transient, 
            Func<AssemblyScanner.AssemblyScanResult, bool> filter = null) 
        {
            return services.AddValidatorsFromAssembly(Assembly.GetCallingAssembly(), lifetime, filter);
        }
        
        /// <summary>
        /// Adds all validators in specified assemblies
        /// </summary>
        /// <param name="services">The collection of services</param>
        /// <param name="assemblies">The assemblies to scan</param>
        /// <param name="lifetime">The lifetime of the validators. The default is transient</param>
        /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddValidatorsFromAssemblies(this IServiceCollection services, 
            IEnumerable<Assembly> assemblies, 
            ServiceLifetime lifetime = ServiceLifetime.Transient, 
            Func<AssemblyScanner.AssemblyScanResult, bool> filter = null) 
        {
            foreach (var assembly in assemblies)
                services.AddValidatorsFromAssembly(assembly, lifetime, filter);
            return services;
        }
        
        /// <summary>
        /// Adds all validators in specified assembly
        /// </summary>
        /// <param name="services">The collection of services</param>
        /// <param name="assembly">The assembly to scan</param>
        /// <param name="lifetime">The lifetime of the validators. The default is transient</param>
        /// <param name="filter">Optional filter that allows certain types to be skipped from registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddValidatorsFromAssembly(this IServiceCollection services, 
            Assembly assembly, 
            ServiceLifetime lifetime = ServiceLifetime.Transient, 
            Func<AssemblyScanner.AssemblyScanResult, bool> filter = null) 
        {
            AddGrpcValidatorCore(services);
            foreach (var scanResult in AssemblyScanner.FindValidatorsInAssembly(assembly).Where(filter ?? (_ => true)))
            {
                services.Add(new ServiceDescriptor(scanResult.InterfaceType, scanResult.ValidatorType, lifetime));
                services.Add(new ServiceDescriptor(scanResult.ValidatorType, scanResult.ValidatorType, lifetime));
            }
            return services;
        }


        /// <summary>
        ///     Add all profiles from calling assembly.
        /// </summary>
        /// <param name="services">The collection of services.</param>
        /// <returns></returns>
        public static IServiceCollection AddProfilesFromAssembly(this IServiceCollection services)
        {
            return services.AddProfilesFromAssembly(Assembly.GetCallingAssembly());
        }

        /// <summary>
        ///     Add all profiles from specific assembly.
        /// </summary>
        /// <param name="services">The collection of services.</param>
        /// <param name="assembly">Assembly to find profiles.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IServiceCollection AddProfilesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(type => type.IsClass && typeof(ValidatorProfileBase).IsAssignableFrom(type)))
            {
                if (type.GetConstructors().All(c => c.GetParameters().Length != 0))
                    throw new InvalidOperationException("All profile class should have constructor without any parameters.");

                services.AddValidatorProfile((ValidatorProfileBase) Activator.CreateInstance(type));
            }

            return services;
        }
    }
}
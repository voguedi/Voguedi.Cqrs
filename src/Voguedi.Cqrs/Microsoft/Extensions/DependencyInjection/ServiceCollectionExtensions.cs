using System;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi;
using Voguedi.Commands;
using Voguedi.Domain.Caching;
using Voguedi.Domain.Events;
using Voguedi.Domain.Repositories;
using Voguedi.Messaging;
using Voguedi.Reflection;
using Voguedi.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        #region Private Methods

        static void AddCommandHandlers(IServiceCollection services, params Assembly[] assemblies)
        {
            foreach (var implementationType in new TypeFinder().GetTypesBySpecifiedType(typeof(ICommandHandler<>), assemblies))
            {
                foreach (var serviceType in implementationType.GetTypeInfo().ImplementedInterfaces)
                    services.TryAddEnumerable(ServiceDescriptor.Transient(serviceType, implementationType));
            }
        }

        static void AddEventHandlers(IServiceCollection services, params Assembly[] assemblies)
        {
            foreach (var implementationType in new TypeFinder().GetTypesBySpecifiedType(typeof(IEventHandler<>), assemblies))
            {
                foreach (var serviceType in implementationType.GetTypeInfo().ImplementedInterfaces)
                    services.TryAddEnumerable(ServiceDescriptor.Transient(serviceType, implementationType));
            }
        }

        #endregion

        #region Public Methods

        public static IServiceCollection AddVoguedi(this IServiceCollection services, Action<VoguediOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));
            
            services.TryAddSingleton<ICommandSender, CommandSender>();
            services.TryAddSingleton<ICommandProcessor, CommandProcessor>();
            services.TryAddSingleton<IProcessingCommandHandler, ProcessingCommandHandler>();
            services.TryAddSingleton<IProcessingCommandHandlerContextFactory, ProcessingCommandHandlerContextFactory>();
            services.TryAddSingleton<IProcessingCommandQueueFactory, ProcessingCommandQueueFactory>();

            services.TryAddSingleton<ICommittingEventHandler, CommittingEventHandler>();
            services.TryAddSingleton<ICommittingEventQueueFactory, CommittingEventQueueFactory>();
            services.TryAddSingleton<IEventCommitter, EventCommitter>();
            services.TryAddSingleton<IEventProcessor, EventProcessor>();
            services.TryAddSingleton<IEventPublisher, EventPublisher>();
            services.TryAddSingleton<IEventSubscriber, EventSubscriber>();
            services.TryAddSingleton<IProcessingEventHandler, ProcessingEventHandler>();
            services.TryAddSingleton<IProcessingEventQueueFactory, ProcessingEventQueueFactory>();
            
            services.AddSingleton<ICache, MemoryCache>();
            services.AddSingleton<IRepository, EventSourcedRepository>();
            services.TryAddSingleton<IMessageSubscriptionManager, MessageSubscriptionManager>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IService, MemoryCache>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IService, CommandProcessor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IService, CommandSubscriber>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IService, EventCommitter>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IService, EventProcessor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IService, EventSubscriber>());

            services.TryAddSingleton<IBootstrapper, Bootstrapper>();
            services.AddTransient<IStartupFilter, StartupFilter>();

            services.AddJson();

            var options = new VoguediOptions();
            setupAction(options);

            foreach (var registrar in options.Registrars)
                registrar.Register(services);

            var assemblies = options.Assemblies;
            AddCommandHandlers(services, assemblies);
            AddEventHandlers(services, assemblies);
            services.AddUitls();
            services.AddSingleton(options);
            return services;
        }

        #endregion
    }
}

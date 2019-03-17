using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Voguedi;
using Voguedi.ApplicationMessages;
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

        static void AddCommandHandlers(IServiceCollection services, TypeFinder typeFinder, params Assembly[] assemblies)
        {
            foreach (var implementationType in typeFinder.GetTypesBySpecifiedType(typeof(ICommandHandler<>), assemblies))
            {
                foreach (var serviceType in implementationType.GetTypeInfo().ImplementedInterfaces)
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
            }
        }

        static void AddCommandAsyncHandlers(IServiceCollection services, TypeFinder typeFinder, params Assembly[] assemblies)
        {
            foreach (var implementationType in typeFinder.GetTypesBySpecifiedType(typeof(ICommandAsyncHandler<>), assemblies))
            {
                foreach (var serviceType in implementationType.GetTypeInfo().ImplementedInterfaces)
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
            }
        }

        static void AddApplicationMessageHandlers(IServiceCollection services, TypeFinder typeFinder, params Assembly[] assemblies)
        {
            foreach (var implementationType in typeFinder.GetTypesBySpecifiedType(typeof(IApplicationMessageHandler<>), assemblies))
            {
                foreach (var serviceType in implementationType.GetTypeInfo().ImplementedInterfaces)
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
            }
        }

        static void AddEventHandlers(IServiceCollection services, TypeFinder typeFinder, params Assembly[] assemblies)
        {
            foreach (var implementationType in typeFinder.GetTypesBySpecifiedType(typeof(IEventHandler<>), assemblies))
            {
                foreach (var serviceType in implementationType.GetTypeInfo().ImplementedInterfaces)
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(serviceType, implementationType));
            }
        }

        static void AddSubscriberServices(IServiceCollection services, TypeFinder typeFinder, params Assembly[] assemblies)
        {
            var serviceType = typeof(ISubscriberService);

            foreach (var implementationType in typeFinder.GetTypesBySpecifiedType(serviceType, assemblies))
                services.TryAddEnumerable(ServiceDescriptor.Singleton(serviceType, implementationType));
        }

        static void AddBackgroundWorkerServices(IServiceCollection services, TypeFinder typeFinder, params Assembly[] assemblies)
        {
            var serviceType = typeof(IBackgroundWorkerService);

            foreach (var implementationType in typeFinder.GetTypesBySpecifiedType(serviceType, assemblies))
                services.TryAddEnumerable(ServiceDescriptor.Singleton(serviceType, implementationType));
        }

        static void AddStoreServices(IServiceCollection services, TypeFinder typeFinder, params Assembly[] assemblies)
        {
            var serviceType = typeof(IStoreService);

            foreach (var implementationType in typeFinder.GetTypesBySpecifiedType(serviceType, assemblies))
                services.TryAddEnumerable(ServiceDescriptor.Singleton(serviceType, implementationType));
        }

        #endregion

        #region Public Methods

        public static IServiceCollection AddVoguedi(this IServiceCollection services, Action<VoguediOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            services.TryAddSingleton<IMessageSubscriptionManager, MessageSubscriptionManager>();
            services.TryAddSingleton<IMessagePublisher, MessagePublisher>();

            services.TryAddSingleton<ICommandSender, CommandSender>();
            services.TryAddSingleton<ICommandSubscriber, CommandSubscriber>();
            services.TryAddSingleton<ICommandProcessor, CommandProcessor>();
            services.TryAddSingleton<IProcessingCommandHandler, ProcessingCommandHandler>();
            services.TryAddSingleton<IProcessingCommandHandlerContextFactory, ProcessingCommandHandlerContextFactory>();
            services.TryAddSingleton<IProcessingCommandQueueFactory, ProcessingCommandQueueFactory>();

            services.TryAddSingleton<IApplicationMessagePublisher, ApplicationMessagePublisher>();
            services.TryAddSingleton<IApplicationMessageProcessor, ApplicationMessageProcessor>();
            services.TryAddSingleton<IApplicationMessageSubscriber, ApplicationMessageSubscriber>();
            services.TryAddSingleton<IProcessingApplicationMessageHandler, ProcessingApplicationMessageHandler>();
            services.TryAddSingleton<IProcessingApplicationMessageQueueFactory, ProcessingApplicationMessageQueueFactory>();

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
            services.AddTransient<IHostedService, Bootstrapper>();
            services.AddJson();
            services.AddUitls();

            var options = new VoguediOptions();
            setupAction(options);
            services.AddSingleton(options);

            foreach (var registrar in options.Registrars)
                registrar.Register(services);

            var assemblies = options.Assemblies;
            services.AddDependencyServices(assemblies);
            var typeFinder = new TypeFinder();
            AddCommandHandlers(services, typeFinder, assemblies);
            AddCommandAsyncHandlers(services, typeFinder, assemblies);
            AddApplicationMessageHandlers(services, typeFinder, assemblies);
            AddEventHandlers(services, typeFinder, assemblies);
            AddSubscriberServices(services, typeFinder, assemblies);
            AddBackgroundWorkerServices(services, typeFinder, assemblies);
            AddStoreServices(services, typeFinder, assemblies);
            return services;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Voguedi;
using Voguedi.Commands;
using Voguedi.Domain.Events;
using Voguedi.Domain.Repositories;
using Voguedi.Messaging;
using Voguedi.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        #region Private Methods

        static IEnumerable<Type> GetImplementations(Type service) => TypeFinder.Instance.GetTypes().Where(t => t.IsClass && !t.IsAbstract && service.IsAssignableFrom(t));

        static IEnumerable<Type> GetServices(Type service, Type implementation)
            => implementation.GetTypeInfo().GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == service);

        static void AddCommandHandler(IServiceCollection services)
        {
            foreach (var implementation in GetImplementations(typeof(ICommandHandler)))
            {
                foreach (var service in GetServices(typeof(ICommandHandler<>), implementation))
                    services.TryAddTransient(service, implementation);
            }
        }

        static void AddEventHandler(IServiceCollection services)
        {
            foreach (var implementation in GetImplementations(typeof(IEventHandler)))
            {
                foreach (var service in GetServices(typeof(IEventHandler<>), implementation))
                    services.TryAddTransient(service, implementation);
            }
        }

        #endregion

        #region Public Methods

        public static IServiceCollection AddVoguedi(this IServiceCollection services, Action<VoguediOptions> setupAction)
        {
            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            services.AddDependencyServices();

            services.TryAddSingleton<ICommandBus, CommandBus>();
            services.TryAddSingleton<ICommandSender, CommandBus>();
            services.TryAddSingleton<ICommandProcessor, CommandProcessor>();
            services.TryAddSingleton<IProcessingCommandHandler, ProcessingCommandHandler>();
            services.TryAddSingleton<IProcessingCommandHandlerContextFactory, ProcessingCommandHandlerContextFactory>();
            services.TryAddSingleton<IProcessingCommandQueueFactory, ProcessingCommandQueueFactory>();
            AddCommandHandler(services);

            services.TryAddSingleton<IRepository, EventSourcedRepository>();
            services.TryAddSingleton<ICommittingEventHandler, CommittingEventHandler>();
            services.TryAddSingleton<ICommittingEventQueueFactory, CommittingEventQueueFactory>();
            services.TryAddSingleton<IEventCommitter, EventCommitter>();
            services.TryAddSingleton<IEventProcessor, EventProcessor>();
            services.TryAddSingleton<IEventPublisher, EventPublisher>();
            services.TryAddSingleton<IEventSubscriber, EventSubscriber>();
            services.TryAddSingleton<IProcessingEventHandler, ProcessingEventHandler>();
            services.TryAddSingleton<IProcessingEventQueueFactory, ProcessingEventQueueFactory>();
            AddEventHandler(services);

            services.TryAddSingleton<IMessageQueueTopicProvider, MessageQueueTopicProvider>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageService, CommandProcessor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageService, CommandSubscriber>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageService, EventCommitter>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageService, EventProcessor>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMessageService, EventSubscriber>());

            services.TryAddSingleton<IBootstrapper, Bootstrapper>();
            services.AddTransient<IStartupFilter, StartupFilter>();

            var options = new VoguediOptions();
            setupAction(options);

            foreach (var registrar in options.Registrars)
                registrar.Register(services);

            services.AddSingleton(options);
            return services;
        }

        #endregion
    }
}

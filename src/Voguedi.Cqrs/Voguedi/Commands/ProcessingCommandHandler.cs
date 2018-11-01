using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voguedi.Domain.Events;

namespace Voguedi.Commands
{
    class ProcessingCommandHandler : IProcessingCommandHandler
    {
        #region Private Fields

        readonly IProcessingCommandHandlerContextFactory contextFactory;
        readonly IDomainEventCommitter eventCommitter;
        readonly IDomainEventStore eventStore;
        readonly IDomainEventPublisher eventPublisher;
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;
        readonly ConcurrentDictionary<Type, ICommandHandler> handlerMapping = new ConcurrentDictionary<Type, ICommandHandler>();

        #endregion

        #region Ctors

        public ProcessingCommandHandler(
            IProcessingCommandHandlerContextFactory contextFactory,
            IDomainEventCommitter eventCommitter,
            IDomainEventStore eventStore,
            IDomainEventPublisher eventPublisher,
            IServiceProvider serviceProvider,
            ILogger<ProcessingCommandHandler> logger)
        {
            this.contextFactory = contextFactory;
            this.eventCommitter = eventCommitter;
            this.eventStore = eventStore;
            this.eventPublisher = eventPublisher;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        #endregion

        #region Private Methods

        async Task HandleCommandAsync(ProcessingCommand processingCommand, ICommandHandler handler)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();
            var handlerType = handler.GetType();
            var context = contextFactory.Create();
            var handled = false;

            try
            {
                var handlerMethod = handlerType.GetTypeInfo().GetMethod("HandleAsync", new[] { context.GetType(), command.GetType() });
                await (Task)handlerMethod.Invoke(handler, new object[] { context, command });
                logger.LogInformation($"命令处理器执行成功！ [CommandType = {command.GetType()}, CommandId = {command.Id}, CommandHandlerType = {handlerType}]");
                handled = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"命令处理器执行失败！ [CommandType = {command.GetType()}, CommandId = {command.Id}, CommandHandlerType = {handlerType}]");
                await TryGetAndPublishEventStreamAsync(processingCommand);
            }

            if (handled)
                await TryGetAndCommitEvent(processingCommand, context);
        }

        Task TryGetAndHandleCommandAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();
            var handlerType = typeof(ICommandHandler<>).GetTypeInfo().MakeGenericType(commandType);

            using (var serviceScope = serviceProvider.CreateScope())
            {
                var handlers = serviceScope.ServiceProvider.GetServices(handlerType)?.Cast<ICommandHandler>();

                if (handlers?.Count() == 1)
                {
                    var handler = handlers.First();
                    handlerMapping[commandType] = handler;
                    return HandleCommandAsync(processingCommand, handler);
                }

                if (handlers?.Count() > 1)
                {
                    logger.LogError($"命令注册超过1个处理器！ [CommandType = {commandType}, CommandId = {command.Id}, CommandHandlerTypes = [{string.Join(" | ", handlers.Select(item => item.GetType()))}]]");
                    return processingCommand.OnQueueRejectedAsync();
                }

                logger.LogError($"命令未注册任何处理器！ [CommandType = {commandType}, CommandId = {command.Id}]");
                return processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task TryGetAndPublishEventStreamAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var aggregateRootId = command.GetAggregateRootId();
            var result = await eventStore.GetStreamAsync(command.GetAggregateRootId(), command.Id);

            if (result.Succeeded)
            {
                var eventStream = result.Data;

                if (eventStream != null)
                {
                    logger.LogInformation($"获取命令产生的领域事件成功！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}, DomainEventStream = {eventStream}]");
                    await PublishEventStreamAsync(processingCommand, eventStream);
                }
                else
                {
                    logger.LogError($"未获取到任何命令产生的领域事件！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}]");
                    await processingCommand.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取命令产生的领域事件失败！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task PublishEventStreamAsync(ProcessingCommand processingCommand, DomainEventStream eventStream)
        {
            var command = processingCommand.Command;
            var result = await eventPublisher.PublisheAsync(eventStream);

            if (result.Succeeded)
            {
                logger.LogInformation($"命令产生的领域事件发布成功！ [CommandType = {command.GetType()}, CommandId = {command.Id}, DomainEventStream = {eventStream}]");
                await processingCommand.OnQueueCommittedAsync();
            }
            else
            {
                logger.LogInformation(result.Exception, $"命令产生的领域事件发布失败！ [CommandType = {command.GetType()}, CommandId = {command.Id}, DomainEventStream = {eventStream}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task TryGetAndCommitEvent(ProcessingCommand processingCommand, IProcessingCommandHandlerContext context)
        {
            var command = processingCommand.Command;
            var aggregateRoots = context.GetAggregateRoots().Where(a => a.GetUncommittedEvents().Any());

            if (aggregateRoots?.Count() == 1)
            {
                var aggregateRoot = aggregateRoots.First();
                var eventStream = new DomainEventStream(
                    command.Id,
                    aggregateRoot.GetTypeName(),
                    aggregateRoot.GetId(),
                    aggregateRoot.GetVersion() + 1,
                    aggregateRoot.GetUncommittedEvents());
                var committingEvent = new CommittingDomainEvent(eventStream, processingCommand, aggregateRoot);
                eventCommitter.Commit(committingEvent);
            }
            else if (aggregateRoots?.Count() > 1)
            {
                logger.LogError($"命令处理超过1个聚合根！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRoots = [{string.Join(" | ", aggregateRoots.Select(a => $"Type = {a.GetType()}, Id = {a.GetId()}"))}]]");
                await processingCommand.OnQueueRejectedAsync();
            }
            else
            {
                logger.LogWarning($"命令未处理任何聚合根！ [CommandType = {command.GetType()}, CommandId = {command.Id}]");
                await TryGetAndPublishEventStreamAsync(processingCommand);
            }
        }

        #endregion

        #region IProcessingCommandHandler

        public Task HandleAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();
            var aggregateRootId = command.GetAggregateRootId();

            if (string.IsNullOrWhiteSpace(aggregateRootId))
            {
                logger.LogError($"命令处理的聚合根 Id 不能为空！ [CommandType = {commandType}, CommandId = {command.Id}]");
                return processingCommand.OnQueueRejectedAsync();
            }

            if (handlerMapping.TryGetValue(commandType, out var handler))
            {
                if (handler != null)
                    return HandleCommandAsync(processingCommand, handler);

                handlerMapping.TryRemove(commandType);
            }

            return TryGetAndHandleCommandAsync(processingCommand);
        }

        #endregion
    }
}

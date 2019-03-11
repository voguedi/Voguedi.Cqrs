using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Voguedi.ApplicationMessages;
using Voguedi.AsyncExecution;
using Voguedi.Domain.Events;

namespace Voguedi.Commands
{
    class ProcessingCommandHandler : IProcessingCommandHandler
    {
        #region Private Fields

        readonly IProcessingCommandHandlerContextFactory contextFactory;
        readonly IEventCommitter eventCommitter;
        readonly IEventStore eventStore;
        readonly IEventPublisher eventPublisher;
        readonly IApplicationMessagePublisher applicationMessagePublisher;
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;
        readonly ConcurrentDictionary<Type, ICommandHandler> handlerMapping = new ConcurrentDictionary<Type, ICommandHandler>();
        readonly ConcurrentDictionary<Type, ICommandAsyncHandler> asyncHandlerMapping = new ConcurrentDictionary<Type, ICommandAsyncHandler>();

        #endregion

        #region Ctors

        public ProcessingCommandHandler(
            IProcessingCommandHandlerContextFactory contextFactory,
            IEventCommitter eventCommitter,
            IEventStore eventStore,
            IEventPublisher eventPublisher,
            IApplicationMessagePublisher applicationMessagePublisher,
            IServiceProvider serviceProvider,
            ILogger<ProcessingCommandHandler> logger)
        {
            this.contextFactory = contextFactory;
            this.eventCommitter = eventCommitter;
            this.eventStore = eventStore;
            this.eventPublisher = eventPublisher;
            this.applicationMessagePublisher = applicationMessagePublisher;
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

            try
            {
                var context = contextFactory.Create();
                var handlerMethod = handlerType.GetTypeInfo().GetMethod("HandleAsync", new[] { context.GetType(), commandType });
                await (Task)handlerMethod.Invoke(handler, new object[] { context, command });
                logger.LogInformation($"命令处理器执行成功！ [CommandType = {commandType}, CommandId = {command.Id}, CommandHandlerType = {handlerType}]");
                await TryGetAndCommitEvent(processingCommand, context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"命令处理器执行失败，尝试获取已存储的事件！ [CommandType = {commandType}, CommandId = {command.Id}, CommandHandlerType = {handlerType}]");
                await TryGetAndPublishEventStreamAsync(processingCommand);
            }
        }

        Task TryGetAndHandleCommandAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();

            using (var serviceScope = serviceProvider.CreateScope())
            {
                var handlerType = typeof(ICommandHandler<>).GetTypeInfo().MakeGenericType(commandType);
                var handlers = serviceScope.ServiceProvider.GetServices(handlerType)?.Cast<ICommandHandler>();

                if (handlers?.Count() == 1)
                {
                    var handler = handlers.First();
                    handlerMapping[commandType] = handler;
                    return HandleCommandAsync(processingCommand, handler);
                }

                if (handlers?.Count() > 1)
                {
                    logger.LogError($"命令注册超过 1 个处理器！ [CommandType = {commandType}, CommandId = {command.Id}, CommandHandlerTypes = [{string.Join(" | ", handlers.Select(item => item.GetType()))}]]");
                    return processingCommand.OnQueueRejectedAsync();
                }

                return TryGetAndAsyncHandleCommandAsync(processingCommand);
            }
        }

        Task TryGetAndCommitEvent(ProcessingCommand processingCommand, IProcessingCommandHandlerContext context)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();
            var commandId = command.Id;
            var aggregateRoots = context.GetAggregateRoots().Where(a => a.GetUncommittedEvents().Any());

            if (aggregateRoots?.Count() == 1)
            {
                var aggregateRoot = aggregateRoots.First();
                var aggregateRootType = aggregateRoot.GetAggregateRootType();
                var aggregateRootId = aggregateRoot.GetAggregateRootId();
                var eventStream = new EventStream(
                    commandId,
                    aggregateRootType.FullName,
                    aggregateRootId,
                    aggregateRoot.Version + 1,
                    aggregateRoot.GetUncommittedEvents());
                var committingEvent = new CommittingEvent(eventStream, processingCommand, aggregateRoot);
                logger.LogInformation($"获取命令处理的聚合根成功！ [CommandType = {commandType}, CommandId = {commandId}, AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return eventCommitter.CommitAsync(committingEvent);
            }

            if (aggregateRoots?.Count() > 1)
            {
                logger.LogError($"命令处理超过1个聚合根！ [CommandType = {commandType}, CommandId = {commandId}, AggregateRoots = [{string.Join(" | ", aggregateRoots.Select(c => $"Type = {c.GetAggregateRootType()}, Id = {c.GetAggregateRootId()}"))}]]");
                return processingCommand.OnQueueRejectedAsync();
            }

            logger.LogError($"命令未处理任何聚合根！ [CommandType = {commandType}, CommandId = {commandId}]");
            return TryGetAndPublishEventStreamAsync(processingCommand);
        }

        async Task TryGetAndPublishEventStreamAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandId = command.Id;
            var aggregateRootId = command.AggregateRootId;
            var result = await eventStore.GetByCommandIdAsync(aggregateRootId, commandId);

            if (result.Succeeded)
            {
                var eventStream = result.Data;

                if (eventStream != null)
                {
                    logger.LogInformation($"获取已存储的事件成功！ [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}, EventStream = {eventStream}]");
                    await PublishEventStreamAsync(processingCommand, eventStream);
                }
                else
                {
                    logger.LogError($"未获取到任何已存储的事件！ [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}]");
                    await processingCommand.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取已存储的事件失败！ [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task PublishEventStreamAsync(ProcessingCommand processingCommand, EventStream eventStream)
        {
            var command = processingCommand.Command;
            var result = await eventPublisher.PublishStreamAsync(eventStream);

            if (result.Succeeded)
            {
                logger.LogInformation($"事件发布成功！ [CommandType = {command.GetType()}, CommandId = {command.Id}, EventStream = {eventStream}]");
                await processingCommand.OnQueueCommittedAsync();
            }
            else
            {
                logger.LogError(result.Exception, $"事件发布失败！ [CommandType = {command.GetType()}, CommandId = {command.Id}, EventStream = {eventStream}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        Task TryGetAndAsyncHandleCommandAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();

            if (asyncHandlerMapping.TryGetValue(commandType, out var asyncHandler))
                return AsyncHandleCommandAsync(processingCommand, asyncHandler);

            using (var serviceScope = serviceProvider.CreateScope())
            {
                var asyncHandlerType = typeof(ICommandAsyncHandler<>).GetTypeInfo().MakeGenericType(commandType);
                var asyncHandlers = serviceScope.ServiceProvider.GetServices(asyncHandlerType)?.Cast<ICommandAsyncHandler>();

                if (asyncHandlers?.Count() == 1)
                {
                    asyncHandler = asyncHandlers.First();
                    asyncHandlerMapping[commandType] = asyncHandler;
                    return AsyncHandleCommandAsync(processingCommand, asyncHandler);
                }

                if (asyncHandlers?.Count() > 1)
                {
                    logger.LogError($"命令注册超过 1 个异步处理器！ [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerTypes = [{string.Join(" | ", asyncHandlers.Select(item => item.GetType()))}]]");
                    return processingCommand.OnQueueRejectedAsync();
                }

                logger.LogError($"命令未注册任何同步或异步处理器！ [CommandType = {commandType}, CommandId = {command.Id}]");
                return processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task AsyncHandleCommandAsync(ProcessingCommand processingCommand, ICommandAsyncHandler asyncHandler)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();
            var asyncHandlerType = asyncHandler.GetType();

            try
            {
                var asyncHandlerMethod = asyncHandlerType.GetTypeInfo().GetMethod("HandleAsync", new[] { commandType });
                var result = await (Task<AsyncExecutedResult<IApplicationMessage>>)asyncHandlerMethod.Invoke(asyncHandler, new [] { command });

                if (result.Succeeded)
                {
                    var applicationMessage = result.Data;

                    if (applicationMessage != null)
                    {
                        logger.LogInformation($"命令异步处理器执行成功，发布产生的应用消息！ [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}, ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageRoutingKey = {applicationMessage.GetRoutingKey()}]");
                        await PublishApplicationMessageAsync(processingCommand, applicationMessage);
                    }
                    else
                    {
                        logger.LogInformation($"命令异步处理器执行成功，未产生任何应用消息！ [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}]");
                        await processingCommand.OnQueueCommittedAsync();
                    }
                }
                else
                {
                    logger.LogError($"命令异步处理器执行失败！ [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}]");
                    await processingCommand.OnQueueRejectedAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"命令异步处理器执行失败！ [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task PublishApplicationMessageAsync(ProcessingCommand processingCommand, IApplicationMessage applicationMessage)
        {
            var result = await applicationMessagePublisher.PublishAsync(applicationMessage);

            if (result.Succeeded)
            {
                logger.LogInformation($"命令异步处理器执行产生的应用消息发布成功！ [CommandType = {processingCommand.Command.GetType()}, CommandId = {processingCommand.Command.Id}, ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageRoutingKey = {applicationMessage.GetRoutingKey()}]");
                await processingCommand.OnQueueCommittedAsync();
            }
            else
            {
                logger.LogInformation(result.Exception, $"命令异步处理器执行产生的应用消息发布失败！ [CommandType = {processingCommand.Command.GetType()}, CommandId = {processingCommand.Command.Id}, ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageRoutingKey = {applicationMessage.GetRoutingKey()}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        #endregion

        #region IProcessingCommandHandler

        public Task HandleAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();

            if (string.IsNullOrWhiteSpace(command.AggregateRootId))
            {
                logger.LogError($"命令处理的聚合根 Id 不能为空！ [CommandType = {commandType}, CommandId = {command.Id}]");
                return processingCommand.OnQueueRejectedAsync();
            }

            if (handlerMapping.TryGetValue(commandType, out var handler))
                return HandleCommandAsync(processingCommand, handler);

            return TryGetAndHandleCommandAsync(processingCommand);
        }

        #endregion
    }
}

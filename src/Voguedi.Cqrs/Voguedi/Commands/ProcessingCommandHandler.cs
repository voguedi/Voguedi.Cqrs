using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Voguedi.ApplicationMessages;
using Voguedi.Domain.Events;
using Voguedi.Infrastructure;
using Voguedi.ObjectSerializers;

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
        readonly IStringObjectSerializer objectSerializer;
        readonly IServiceProvider serviceProvider;
        readonly ILogger logger;
        readonly ConcurrentDictionary<Type, ICommandHandler> handlerMapping;
        readonly ConcurrentDictionary<Type, ICommandAsyncHandler> asyncHandlerMapping;

        #endregion

        #region Ctors

        public ProcessingCommandHandler(
            IProcessingCommandHandlerContextFactory contextFactory,
            IEventCommitter eventCommitter,
            IEventStore eventStore,
            IEventPublisher eventPublisher,
            IApplicationMessagePublisher applicationMessagePublisher,
            IStringObjectSerializer objectSerializer,
            IServiceProvider serviceProvider,
            ILogger<ProcessingCommandHandler> logger)
        {
            this.contextFactory = contextFactory;
            this.eventCommitter = eventCommitter;
            this.eventStore = eventStore;
            this.eventPublisher = eventPublisher;
            this.applicationMessagePublisher = applicationMessagePublisher;
            this.objectSerializer = objectSerializer;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            handlerMapping = new ConcurrentDictionary<Type, ICommandHandler>();
            asyncHandlerMapping = new ConcurrentDictionary<Type, ICommandAsyncHandler>();
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
                logger.LogDebug($"命令处理器执行成功，尝试获取已产生的事件并提交。 [CommandType = {commandType}, CommandId = {command.Id}, CommandHandlerType = {handlerType}]");
                await TryGetAndCommitEvent(processingCommand, context);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"命令处理器执行失败，尝试获取已存储的事件。 [CommandType = {commandType}, CommandId = {command.Id}, CommandHandlerType = {handlerType}]");
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
                    logger.LogError($"注册超过 1 个命令处理器。 [CommandType = {commandType}, CommandId = {command.Id}, CommandHandlerTypes = [{string.Join(" | ", handlers.Select(item => item.GetType()))}]]");
                    return processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "注册超过 1 个命令处理器。");
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
                var aggregateRootType = aggregateRoot.GetType();
                var aggregateRootId = aggregateRoot.Id;
                var eventStream = new EventStream(
                    commandId,
                    aggregateRootType.FullName,
                    aggregateRootId,
                    aggregateRoot.Version + 1,
                    aggregateRoot.GetUncommittedEvents());
                var committingEvent = new CommittingEvent(eventStream, processingCommand, aggregateRoot);
                logger.LogDebug($"获取命令产生的事件成功，提交并发布事件！");
                return eventCommitter.CommitAsync(committingEvent);
            }

            if (aggregateRoots?.Count() > 1)
            {
                logger.LogError($"命令处理超过 1 个聚合根。 [CommandType = {commandType}, CommandId = {commandId}, AggregateRoots = [{string.Join(" | ", aggregateRoots.Select(a => $"Type = {a.GetType()}, Id = {a.Id}"))}]]");
                return processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "命令处理超过 1 个聚合根。");
            }

            logger.LogWarning($"命令未处理任何聚合根，尝试获取是否已有事件产生并发布。 [CommandType = {commandType}, CommandId = {commandId}]");
            return TryGetAndPublishEventStreamAsync(processingCommand);
        }

        async Task TryGetAndPublishEventStreamAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandId = command.Id;
            var aggregateRootId = command.AggregateRootId;
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult<EventStream>>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"获取已产生的事件失败，重试。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => eventStore.GetByCommandIdAsync(aggregateRootId, commandId));

            if (result.Succeeded)
            {
                var eventStream = result.Data;

                if (eventStream != null)
                {
                    logger.LogDebug($"获取已产生的事件成功，发布事件。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}, EventStream = {eventStream}]");
                    await PublishEventStreamAsync(processingCommand, eventStream);
                }
                else
                {
                    logger.LogWarning($"未获取到任何已产生的事件，命令未处理任何聚合根。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}]");
                    await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.NothingChanged, "未获取到任何已产生的事件，命令未处理任何聚合根。");
                }
            }
            else
                logger.LogError(result.Exception, $"获取已产生的事件失败。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}]");
        }

        async Task PublishEventStreamAsync(ProcessingCommand processingCommand, EventStream eventStream)
        {
            var command = processingCommand.Command;
            
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"命令产生的事件发布失败，重试。 [CommandType = {command.GetType()}, CommandId = {command.Id}, EventStream = {eventStream}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => eventPublisher.PublishStreamAsync(eventStream));

            if (result.Succeeded)
            {
                logger.LogDebug($"命令产生的事件发布成功。 [CommandType = {command.GetType()}, CommandId = {command.Id}, EventStream = {eventStream}]");
                await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Succeeded);
            }
            else
                logger.LogError(result.Exception, $"命令产生的事件发布失败。 [CommandType = {command.GetType()}, CommandId = {command.Id}, EventStream = {eventStream}]");
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
                    logger.LogError($"注册超过 1 个命令异步处理器。 [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerTypes = [{string.Join(" | ", asyncHandlers.Select(item => item.GetType()))}]]");
                    return processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "注册超过 1 个命令异步处理器。");
                }
                
                logger.LogError($"未注册任何同步或命令异步处理器。 [CommandType = {commandType}, CommandId = {command.Id}]");
                return processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "未注册任何同步或异步命令处理器。");
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
                        logger.LogDebug($"异步命令处理器执行成功，发布产生的应用消息。 [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}, ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageRoutingKey = {applicationMessage.GetRoutingKey()}]");
                        await PublishApplicationMessageAsync(processingCommand, applicationMessage);
                    }
                    else
                    {
                        logger.LogDebug($"异步命令处理器执行成功，未产生任何应用消息。 [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}]");
                        await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Succeeded, "命令异步处理器执行成功，未产生任何应用消息。");
                    }
                }
                else
                {
                    logger.LogError($"异步命令处理器执行失败。 [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}]");
                    await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "异步命令处理器执行失败。");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"异步命令处理器执行失败。 [CommandType = {commandType}, CommandId = {command.Id}, CommandAsyncHandlerType = {asyncHandlerType}]");
                await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "异步命令处理器执行失败。");
            }
        }

        async Task PublishApplicationMessageAsync(ProcessingCommand processingCommand, IApplicationMessage applicationMessage)
        {
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"命令产生的应用消息发布失败，重试。 [CommandType = {processingCommand.Command.GetType()}, CommandId = {processingCommand.Command.Id}, ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageRoutingKey = {applicationMessage.GetRoutingKey()}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => applicationMessagePublisher.PublishAsync(applicationMessage));

            if (result.Succeeded)
            {
                logger.LogDebug($"命令产生的应用消息发布成功。 [CommandType = {processingCommand.Command.GetType()}, CommandId = {processingCommand.Command.Id}, ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageRoutingKey = {applicationMessage.GetRoutingKey()}]");
                await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Succeeded, objectSerializer.Serialize(applicationMessage), applicationMessage.GetTag());
            }
            else
                logger.LogDebug(result.Exception, $"命令产生的应用消息发布失败。 [CommandType = {processingCommand.Command.GetType()}, CommandId = {processingCommand.Command.Id}, ApplicationMessageType = {applicationMessage.GetType()}, ApplicationMessageId = {applicationMessage.Id}, ApplicationMessageRoutingKey = {applicationMessage.GetRoutingKey()}]");
        }

        #endregion

        #region IProcessingCommandHandler

        public Task HandleAsync(ProcessingCommand processingCommand)
        {
            var command = processingCommand.Command;
            var commandType = command.GetType();

            if (string.IsNullOrWhiteSpace(command.AggregateRootId))
            {
                logger.LogError($"命令处理的聚合根 Id 不能为空。 [CommandType = {commandType}, CommandId = {command.Id}]");
                return processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "命令处理的聚合根 Id 不能为空。");
            }

            if (handlerMapping.TryGetValue(commandType, out var handler))
                return HandleCommandAsync(processingCommand, handler);

            return TryGetAndHandleCommandAsync(processingCommand);
        }

        #endregion
    }
}

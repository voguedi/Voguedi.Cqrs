using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Voguedi.Commands;
using Voguedi.Domain.Caching;
using Voguedi.Domain.Repositories;
using Voguedi.Infrastructure;

namespace Voguedi.Domain.Events
{
    class CommittingEventHandler : ICommittingEventHandler
    {
        #region Private Fields

        readonly IEventStore store;
        readonly IEventPublisher publisher;
        readonly IRepository repository;
        readonly ICache cache;
        readonly ILogger logger;

        #endregion

        #region Ctors

        public CommittingEventHandler(IEventStore store, IEventPublisher publisher, IRepository repository, ICache cache, ILogger<CommittingEventHandler> logger)
        {
            this.store = store;
            this.publisher = publisher;
            this.repository = repository;
            this.cache = cache;
            this.logger = logger;
        }

        #endregion

        #region Private Methods

        async Task PublishStreamAsync(EventStream stream, ProcessingCommand processingCommand)
        {
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"命令产生的事件发布失败，重试。 [EventStream = {stream}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => publisher.PublishStreamAsync(stream));

            if (result.Succeeded)
            {
                logger.LogDebug($"命令产生的事件发布成功。 {stream}");
                await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Succeeded);
            }
            else
                logger.LogError(result.Exception, $"命令产生的事件发布失败。 {stream}");
        }

        async Task ResetProcessingCommandQueueSequenceAsync(CommittingEvent committingEvent, long queueSequence)
        {
            var committingEventQueue = committingEvent.Queue;
            var processingCommand = committingEvent.ProcessingCommand;
            var command = processingCommand.Command;
            var processingCommandQueue = processingCommand.Queue;
            processingCommandQueue.Pause();

            try
            {
                await SetAggregateRootCacheAsync(committingEvent);
                processingCommandQueue.ResetSequence(queueSequence);
                committingEventQueue.Clear();
                logger.LogDebug($"重置命令队列序号成功，重启命令队列。 [AggregateRootId = {command.AggregateRootId}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"重置命令队列序号失败，重启命令队列。 [AggregateRootId = {command.AggregateRootId}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
            }
            finally
            {
                processingCommandQueue.Restart();
            }
        }

        Task SetAggregateRootCacheAsync(CommittingEvent committingEvent)
        {
            var aggregateRoot = committingEvent.AggregateRoot;
            return cache.RefreshAsync(aggregateRoot.GetType(), aggregateRoot.Id);
        }

        async Task TryGetAndPublishStreamAsync(CommittingEvent committingEvent)
        {
            var processingCommand = committingEvent.ProcessingCommand;
            var command = processingCommand.Command;
            var commandId = command.Id;
            var aggregateRootId = committingEvent.AggregateRoot.Id;
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult<EventStream>>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"获取已产生的事件失败，重试。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => store.GetByCommandIdAsync(aggregateRootId, commandId));

            if (result.Succeeded)
            {
                var stream = result.Data;

                if (stream != null)
                {
                    logger.LogDebug($"获取已产生的事件成功，发布事件。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}, EventStream = {stream}]");
                    await PublishStreamAsync(stream, processingCommand);
                }
                else
                {
                    logger.LogWarning($"未获取到任何已产生的事件。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}]");
                    await processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "命令产生的事件存储失败，存在相同的命令，未获取到任何已产生的事件。");
                }
            }
            else
                logger.LogError(result.Exception, $"获取已产生的事件失败。 [CommandType = {command.GetType()}, CommandId = {commandId}, AggregateRootId = {aggregateRootId}]");
        }

        async Task TryGetAndPublishFirstStreamAsync(CommittingEvent committingEvent)
        {
            var stream = committingEvent.Stream;
            var aggregateRootId = stream.AggregateRootId;
            var processingCommand = committingEvent.ProcessingCommand;
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult<EventStream>>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"获取已产生的事件失败，重试。 [AggregateRootType = {committingEvent.AggregateRoot.GetType()}, AggregateRootId = {aggregateRootId}, Version = 1, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => store.GetByVersionAsync(aggregateRootId, 1));

            if (result.Succeeded)
            {
                var firstStream = result.Data;

                if (firstStream != null)
                {
                    var command = processingCommand.Command;

                    if (firstStream.CommandId == command.Id)
                    {
                        logger.LogDebug($"获取已产生的事件成功，重置命令处理队列序号，发布事件。 [AggregateRootType = {committingEvent.AggregateRoot.GetType()}, AggregateRootId = {aggregateRootId}, Version = 1, EventStream = {firstStream}]");
                        await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence)
                            .ContinueWith(c => PublishStreamAsync(firstStream, processingCommand));
                    }
                    else
                    {
                        logger.LogWarning($"获取已产生的事件成功，存在不同命令重复处理同一聚合根，重置命令处理队列序号。 [AggregateRootType = {committingEvent.AggregateRoot.GetType()}, AggregateRootId = {aggregateRootId}, Version = 1, CommittingEventStream = {stream}, StoredEventStream = {firstStream}]");
                        await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence + 1)
                            .ContinueWith(c => processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "命令产生的事件存储失败，存在相同的版本，不同命令重复处理同一聚合根。"));
                    }
                }
                else
                {
                    logger.LogWarning($"未获取任何已产生的事件，重置命令处理队列序号。 [AggregateRootType = {committingEvent.AggregateRoot.GetType()}, AggregateRootId = {aggregateRootId}, Version = 1]");
                    await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence + 1)
                            .ContinueWith(c => processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "命令产生的事件存储失败，存在相同的版本，未获取到任何已产生的事件。"));
                }
            }
            else
                logger.LogError(result.Exception, $"获取已产生的事件失败。 [AggregateRootType = {committingEvent.AggregateRoot.GetType()}, AggregateRootId = {aggregateRootId}, Version = 1]");
        }

        #endregion

        #region ICommittingEventHandler

        public async Task HandleAsync(CommittingEvent committingEvent)
        {
            var stream = committingEvent.Stream;
            var processingCommand = committingEvent.ProcessingCommand;
            var result = await Policy
                .Handle<Exception>()
                .OrResult<AsyncExecutedResult<EventStreamSavedResult>>(r => !r.Succeeded)
                .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (delegateResult, retryCount, retryAttempt) => logger.LogError(delegateResult.Exception ?? delegateResult.Result.Exception, $"命令产生的事件存储失败，重试。 [EventStream = {stream}, RetryCount = {retryCount}, RetryAttempt = {retryAttempt}]"))
                .ExecuteAsync(() => store.SaveAsync(stream));

            if (result.Succeeded)
            {
                var savedResult = result.Data;

                if (savedResult == EventStreamSavedResult.Success)
                {
                    logger.LogDebug($"命令产生的事件存储成功。 {stream}");
                    await PublishStreamAsync(stream, processingCommand);
                    await committingEvent.OnQueueCommitted();
                }
                else if (savedResult == EventStreamSavedResult.DuplicatedCommand)
                {
                    logger.LogWarning($"命令产生的事件存储失败，存在相同的命令，重置命令处理队列序号，尝试获取是否已有事件产生并发布。 {stream}");
                    await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence + 1);
                    await TryGetAndPublishStreamAsync(committingEvent);
                }
                else if (savedResult == EventStreamSavedResult.DuplicatedEvent)
                {
                    if (stream.Version == 1)
                    {
                        logger.LogWarning($"命令产生的事件存储失败，存在相同的版本，尝试获取是否已有事件产生并发布。 {stream}");
                        await TryGetAndPublishFirstStreamAsync(committingEvent);
                    }
                    else
                    {
                        logger.LogWarning($"命令产生的事件存储失败，存在相同的版本，不同命令重复处理同一聚合根，重置命令处理队列序号。 {stream}");
                        await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence)
                            .ContinueWith(c => processingCommand.OnQueueProcessedAsync(CommandExecutedStatus.Failed, "命令产生的事件存储失败，存在相同的版本，不同命令重复处理同一聚合根。"));
                    }
                }
            }
            else
                logger.LogError(result.Exception, $"命令产生的事件存储失败。 {stream}");
        }

        #endregion
    }
}

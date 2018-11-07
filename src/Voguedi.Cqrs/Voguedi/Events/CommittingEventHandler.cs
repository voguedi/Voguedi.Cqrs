using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.Commands;
using Voguedi.Domain.Caching;
using Voguedi.Domain.Repositories;

namespace Voguedi.Events
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
            var result = await publisher.PublishStreamAsync(stream);

            if (result.Succeeded)
            {
                logger.LogInformation($"事件发布成功！ {stream}");
                await processingCommand.OnQueueCommittedAsync();
            }
            else
            {
                logger.LogError(result.Exception, $"事件发布失败！ {stream}");
                await processingCommand.OnQueueRejectedAsync();
            }
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
                logger.LogInformation($"重置命令处理队列成功！ [AggregateRootId = {command.AggregateRootId}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"重置命令处理队列序号失败！ [AggregateRootId = {command.AggregateRootId}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
            }
            finally
            {
                processingCommandQueue.Restart();
            }
        }

        async Task SetAggregateRootCacheAsync(CommittingEvent committingEvent)
        {
            var aggregateRoot = committingEvent.AggregateRoot;
            var aggregateRootType = aggregateRoot.GetType();
            var aggregateRootId = aggregateRoot.GetId();
            var eventSourcedResult = await repository.GetAsync(aggregateRootType, aggregateRootId);

            if (eventSourcedResult.Succeeded)
            {
                var eventSourced = eventSourcedResult.Data;

                if (eventSourced != null)
                {
                    var cacheResult = await cache.SetAsync(eventSourced);

                    if (!cacheResult.Succeeded)
                        logger.LogError(eventSourcedResult.Exception, $"更新聚合根缓存失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                }
            }
            else
                logger.LogError(eventSourcedResult.Exception, $"根据事件溯源获取聚合根最新状态失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
        }

        async Task TryGetAndPublishStreamAsync(CommittingEvent committingEvent)
        {
            var processingCommand = committingEvent.ProcessingCommand;
            var command = processingCommand.Command;
            var aggregateRootId = command.AggregateRootId;
            var result = await store.GetStreamAsync(aggregateRootId, command.Id);

            if (result.Succeeded)
            {
                var stream = result.Data;

                if (stream != null)
                {
                    logger.LogInformation($"获取已存储的事件成功！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}, EventStream = {{stream}}]");
                    await PublishStreamAsync(stream, processingCommand);
                }
                else
                {
                    logger.LogError($"未获取任何已存储的事件！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}]");
                    await processingCommand.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取已存储的事件失败！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task TryGetAndPublishFirstStreamAsync(CommittingEvent committingEvent)
        {
            var stream = committingEvent.Stream;
            var aggregateRootId = stream.AggregateRootId;
            var processingCommand = committingEvent.ProcessingCommand;
            var command = processingCommand.Command;
            var result = await store.GetStreamAsync(stream.AggregateRootId, 1);

            if (result.Succeeded)
            {
                var firstStream = result.Data;

                if (firstStream != null)
                {
                    if (firstStream.CommandId == command.Id)
                    {
                        logger.LogInformation($"获取已存储的事件成功！ [AggregateRootId = {aggregateRootId}, Version = 1, EventStream = {firstStream}]");
                        await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence);
                        await PublishStreamAsync(firstStream, processingCommand);
                    }
                    else
                    {
                        logger.LogError($"存在不同命令重复处理相同聚合根！ [CurrentEventStream = {stream}, ExistsEventStream = {firstStream}]");
                        await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence + 1);
                        await processingCommand.OnQueueRejectedAsync();
                    }
                }
                else
                {
                    logger.LogError($"未获取任何已存储的事件！ [AggregateRootId = {aggregateRootId}, Version = 1]");
                    await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence + 1);
                    await processingCommand.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取已存储的事件失败！ [AggregateRootId = {aggregateRootId}, Version = 1]");
                await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence + 1);
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        #endregion

        #region ICommittingEventHandler

        public async Task HandleAsync(CommittingEvent committingEvent)
        {
            var stream = committingEvent.Stream;
            var processingCommand = committingEvent.ProcessingCommand;
            var result = await store.SaveStreamAsync(stream);

            if (result.Succeeded)
            {
                var savedResult = result.Data;

                if (savedResult == EventStreamSavedResult.Success)
                {
                    logger.LogInformation($"事件存储成功！ {stream}");
                    await PublishStreamAsync(stream, processingCommand);
                }
                else if (savedResult == EventStreamSavedResult.DuplicatedCommand)
                {
                    logger.LogError($"事件存储失败，存在相同的命令！ {stream}");
                    await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence + 1);
                    await TryGetAndPublishStreamAsync(committingEvent);
                }
                else if (savedResult == EventStreamSavedResult.DuplicatedEvent)
                {
                    if (stream.Version == 1)
                    {
                        logger.LogError($"事件存储失败，存在相同的版本！ {stream}");
                        await TryGetAndPublishFirstStreamAsync(committingEvent);
                    }
                    else
                    {
                        logger.LogError($"事件存储失败，存在相同的版本！ {stream}");
                        await ResetProcessingCommandQueueSequenceAsync(committingEvent, processingCommand.QueueSequence);
                        await processingCommand.OnQueueRejectedAsync();
                    }
                }
            }
            else
            {
                logger.LogError(result.Exception, $"事件存储失败！ {stream}");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        #endregion
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.Commands;

namespace Voguedi.Domain.Events
{
    class CommittingDomainEventHandler : ICommittingDomainEventHandler
    {
        #region Private Fields

        readonly IDomainEventStore store;
        readonly IDomainEventPublisher publisher;
        readonly ILogger logger;

        #endregion

        #region Ctors

        public CommittingDomainEventHandler(IDomainEventStore store, IDomainEventPublisher publisher, ILogger<CommittingDomainEventHandler> logger)
        {
            this.store = store;
            this.publisher = publisher;
            this.logger = logger;
        }

        #endregion

        #region Private Methods

        async Task PublishStreamAsync(DomainEventStream stream, ProcessingCommand processingCommand)
        {
            var result = await publisher.PublisheAsync(stream);

            if (result.Succeeded)
            {
                logger.LogInformation($"领域事件发布成功！ {stream}");
                await processingCommand.OnQueueCommittedAsync();
            }
            else
            {
                logger.LogError(result.Exception, $"领域事件发布失败！ {stream}");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        void ResetProcessingCommandQueueSequence(CommittingDomainEvent committingEvent, long queueSequence)
        {
            var committingEventQueue = committingEvent.Queue;
            var processingCommand = committingEvent.ProcessingCommand;
            var command = processingCommand.Command;
            var processingCommandQueue = processingCommand.Queue;
            processingCommandQueue.Pause();

            try
            {
                processingCommandQueue.ResetSequence(queueSequence);
                committingEventQueue.Clear();
                committingEventQueue.Stop();
                logger.LogInformation($"重置命令处理队列成功！ [AggregateRootId = {command.GetAggregateRootId()}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"重置命令处理队列序号失败！ [AggregateRootId = {command.GetAggregateRootId()}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
            }
            finally
            {
                processingCommandQueue.Restart();
            }
        }

        async Task TryGetAndPublishStreamAsync(CommittingDomainEvent committingEvent)
        {
            var processingCommand = committingEvent.ProcessingCommand;
            var command = processingCommand.Command;
            var aggregateRootId = command.GetAggregateRootId();
            var result = await store.GetStreamAsync(aggregateRootId, command.Id);

            if (result.Succeeded)
            {
                var stream = result.Data;

                if (stream != null)
                {
                    logger.LogInformation($"获取已存储的领域事件成功！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}, DomainEventStream = {{stream}}]");
                    await PublishStreamAsync(stream, processingCommand);
                }
                else
                {
                    logger.LogError($"未获取任何已存储的领域事件！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}]");
                    await processingCommand.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取已存储的领域事件失败！ [CommandType = {command.GetType()}, CommandId = {command.Id}, AggregateRootId = {aggregateRootId}]");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        async Task TryGetAndPublishFirstStreamAsync(CommittingDomainEvent committingEvent)
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
                        logger.LogInformation($"获取已存储的领域事件成功！ [AggregateRootId = {aggregateRootId}, Version = 1, DomainEventStream = {firstStream}]");
                        ResetProcessingCommandQueueSequence(committingEvent, processingCommand.QueueSequence);
                        await PublishStreamAsync(firstStream, processingCommand);
                    }
                    else
                    {
                        logger.LogError($"存在不同命令重复处理相同聚合根！ [CurrentDomainEventStream = {stream}, ExistsDomainEventStream = {firstStream}]");
                        ResetProcessingCommandQueueSequence(committingEvent, processingCommand.QueueSequence + 1);
                        await processingCommand.OnQueueRejectedAsync();
                    }
                }
                else
                {
                    logger.LogError($"未获取任何已存储的领域事件！ [AggregateRootId = {aggregateRootId}, Version = 1]");
                    ResetProcessingCommandQueueSequence(committingEvent, processingCommand.QueueSequence + 1);
                    await processingCommand.OnQueueRejectedAsync();
                }
            }
            else
            {
                logger.LogError(result.Exception, $"获取已存储的领域事件失败！");
                ResetProcessingCommandQueueSequence(committingEvent, processingCommand.QueueSequence + 1);
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        #endregion

        #region ICommittingDomainEventHandler

        public async Task HandleAsync(CommittingDomainEvent committingEvent)
        {
            var stream = committingEvent.Stream;
            var processingCommand = committingEvent.ProcessingCommand;
            var result = await store.SaveStreamAsync(stream);

            if (result.Succeeded)
            {
                var savedResult = result.Data;

                if (savedResult == DomainEventStreamSavedResult.Success)
                {
                    logger.LogInformation($"领域事件存储成功！ {stream}");
                    await PublishStreamAsync(stream, processingCommand);
                }
                else if (savedResult == DomainEventStreamSavedResult.DuplicatedCommand)
                {
                    logger.LogError($"领域事件存储失败，存在相同的命令！ {stream}");
                    ResetProcessingCommandQueueSequence(committingEvent, processingCommand.QueueSequence + 1);
                    await TryGetAndPublishStreamAsync(committingEvent);
                }
                else if (savedResult == DomainEventStreamSavedResult.DuplicatedDomainEvent)
                {
                    if (stream.Version == 1)
                    {
                        logger.LogError($"领域事件存储失败，存在相同的版本！");
                        await TryGetAndPublishFirstStreamAsync(committingEvent);
                    }
                    else
                    {
                        logger.LogError($"领域事件存储失败，存在相同的版本！ {stream}");
                        ResetProcessingCommandQueueSequence(committingEvent, processingCommand.QueueSequence);
                        await processingCommand.OnQueueRejectedAsync();
                    }
                }
            }
            else
            {
                logger.LogError(result.Exception, $"领域事件存储失败！ {stream}");
                await processingCommand.OnQueueRejectedAsync();
            }
        }

        #endregion
    }
}

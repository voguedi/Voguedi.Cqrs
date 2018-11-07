using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.AsyncExecution;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Events;

namespace Voguedi.Domain.Repositories
{
    class EventSourcedRepository : IRepository
    {
        #region Private Fields

        readonly IEventStore eventStore;
        readonly ILogger logger;

        #endregion

        #region Ctors
        
        public EventSourcedRepository(IEventStore eventStore, ILogger<EventSourcedRepository> logger)
        {
            this.eventStore = eventStore;
            this.logger = logger;
        }

        #endregion

        #region Private Methods

        IEventSourcedAggregateRoot Build(Type aggregateRootType)
        {
            var obj = FormatterServices.GetUninitializedObject(aggregateRootType);

            if (obj is IEventSourcedAggregateRoot aggregateRoot)
                return aggregateRoot;

            return null;
        }

        #endregion

        #region IRepository

        public async Task<AsyncExecutedResult<IEventSourcedAggregateRoot>> GetAsync(Type aggregateRootType, string aggregateRootId)
        {
            if (aggregateRootType == null)
                throw new ArgumentNullException(nameof(aggregateRootType));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            var aggregateRoot = Build(aggregateRootType);

            if (aggregateRoot != null)
            {
                var result = await eventStore.GetAllAsync(aggregateRootType.FullName, aggregateRootId);

                if (result.Succeeded)
                {
                    var eventStream = result.Data;

                    try
                    {
                        aggregateRoot.ReplayEvents(eventStream);
                        logger.LogInformation($"事件溯源重建聚合根成功！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}, EventStream = {eventStream}]");
                        return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(aggregateRoot);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"事件溯源重建聚合根失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}, EventStream = {eventStream}]");
                    }
                }

                logger.LogError(result.Exception, $"事件溯源重建聚合根失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult<IEventSourcedAggregateRoot>.Failed(result.Exception);
            }

            logger.LogError($"事件溯源重建聚合根失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
            return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(null);
        }

        #endregion
    }
}

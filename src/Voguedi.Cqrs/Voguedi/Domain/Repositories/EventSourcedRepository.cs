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
                var result = await eventStore.GetStreamsAsync(aggregateRoot.GetTypeName(), aggregateRoot.GetId());

                if (result.Succeeded)
                {
                    var eventStream = result.Data;
                    aggregateRoot.ReplayEvents(eventStream);
                    logger.LogInformation($"事件溯源重建聚合根成功！ [AggregateRootType = {aggregateRoot.GetType()}, AggregateRootId = {aggregateRoot.GetId()}, Version = {aggregateRoot.GetVersion()}, EventStream = {eventStream}]");
                    return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(aggregateRoot); ;
                }

                logger.LogError(result.Exception, $"事件溯源重建聚合根失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult<IEventSourcedAggregateRoot>.Failed(result.Exception);
            }

            logger.LogError($"事件溯源重建聚合根失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
            return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(null);
        }

        async Task<AsyncExecutedResult<TAggregateRoot>> IRepository.GetAsync<TAggregateRoot, TIdentity>(TIdentity id)
        {
            if (Equals(id, default(TIdentity)))
                throw new ArgumentNullException(nameof(id));

            var result = await GetAsync(typeof(TAggregateRoot), id.ToString());

            if (result.Succeeded)
            {
                var eventSourced = result.Data;

                if (eventSourced is TAggregateRoot aggregateRoot)
                    return AsyncExecutedResult<TAggregateRoot>.Success(aggregateRoot);

                return AsyncExecutedResult<TAggregateRoot>.Success(null);
            }

            return AsyncExecutedResult<TAggregateRoot>.Failed(result.Exception);
        }

        #endregion
    }
}

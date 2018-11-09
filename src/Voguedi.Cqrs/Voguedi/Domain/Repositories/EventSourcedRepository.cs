using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Voguedi.AsyncExecution;
using Voguedi.Domain.AggregateRoots;
using Voguedi.Domain.Events;

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

        IEventSourcedAggregateRoot Build(Type aggregateRootType, string aggregateRootId)
        {
            var ctors = from ctor in aggregateRootType.GetTypeInfo().GetConstructors()
                        let parameters = ctor.GetParameters()
                        where parameters.Length == 1
                        select ctor;
            var defaultCtor = ctors.FirstOrDefault();

            if (defaultCtor != null)
                return defaultCtor.Invoke(new object[] { aggregateRootId }) as IEventSourcedAggregateRoot;

            throw new Exception($"聚合根 {aggregateRootType} 未提供初始化 Id 的构造函数！");
        }

        #endregion

        #region IRepository

        public async Task<AsyncExecutedResult<IEventSourcedAggregateRoot>> GetAsync(Type aggregateRootType, string aggregateRootId)
        {
            if (aggregateRootType == null)
                throw new ArgumentNullException(nameof(aggregateRootType));

            if (string.IsNullOrWhiteSpace(aggregateRootId))
                throw new ArgumentNullException(nameof(aggregateRootId));

            var aggregateRoot = Build(aggregateRootType, aggregateRootId);

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
                        return AsyncExecutedResult<IEventSourcedAggregateRoot>.Failed(ex);
                    }
                }

                logger.LogError(result.Exception, $"事件溯源重建聚合根失败，未获取任何已存储的事件！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
                return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(null);
            }

            logger.LogError($"事件溯源重建聚合根失败！ [AggregateRootType = {aggregateRootType}, AggregateRootId = {aggregateRootId}]");
            return AsyncExecutedResult<IEventSourcedAggregateRoot>.Success(null);
        }

        #endregion
    }
}

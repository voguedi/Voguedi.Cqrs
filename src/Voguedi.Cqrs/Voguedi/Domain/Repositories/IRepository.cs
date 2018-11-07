﻿using System;
using System.Threading.Tasks;
using Voguedi.Domain.AggregateRoots;
using Voguedi.AsyncExecution;

namespace Voguedi.Domain.Repositories
{
    public interface IRepository
    {
        #region Methods

        Task<AsyncExecutedResult<IEventSourcedAggregateRoot>> GetAsync(Type aggregateRootType, string aggregateRootId);

        #endregion
    }
}

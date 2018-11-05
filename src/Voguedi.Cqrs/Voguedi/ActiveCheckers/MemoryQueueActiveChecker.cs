using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Voguedi.ActiveCheckers
{
    class MemoryQueueActiveChecker : IMemoryQueueActiveChecker
    {
        #region Private Class

        class SchedulerContext
        {
            #region Public Properties

            public string Key { get; set; }

            public Action Action { get; set; }

            public Timer Timer { get; set; }

            public int Eexpiration { get; set; }

            public bool Stopped { get; set; }

            #endregion
        }

        #endregion

        #region Private Fields

        readonly ILogger logger;
        readonly object syncLock = new object();
        readonly ConcurrentDictionary<string, SchedulerContext> contextMapping = new ConcurrentDictionary<string, SchedulerContext>();

        #endregion

        #region Ctors

        public MemoryQueueActiveChecker(ILogger<MemoryQueueActiveChecker> logger) => this.logger = logger;

        #endregion

        #region Private Methods

        void Callback(object state)
        {
            var key = state.ToString();

            if (contextMapping.TryGetValue(key, out var context))
            {
                try
                {
                    if (!context.Stopped)
                    {
                        context.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                        context.Action?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"内存队列存活检查器当前状态异常！ [Key = {context.Key}, Eexpiration = {context.Eexpiration}]");
                }
                finally
                {
                    try
                    {
                        if (!context.Stopped)
                            context.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"内存队列存活检查器当前状态异常！ [Key = {context.Key}, Eexpiration = {context.Eexpiration}]");
                    }
                }
            }
        }

        #endregion

        #region IMemoryQueueActiveChecker

        public void Start(string key, Action action, int expiration)
        {
            lock (syncLock)
            {
                if (!contextMapping.ContainsKey(key))
                {
                    var timer = new Timer(Callback, key, Timeout.Infinite, Timeout.Infinite);
                    contextMapping.TryAdd(
                        key,
                        new SchedulerContext
                        {
                            Action = action,
                            Eexpiration = expiration,
                            Key = key,
                            Stopped = false,
                            Timer = timer
                        });
                    timer.Change(expiration, expiration);
                }
            }
        }

        public void Stop(string key)
        {
            lock (syncLock)
            {
                if (contextMapping.TryGetValue(key, out var context))
                {
                    context.Stopped = true;
                    context.Timer.Dispose();
                    contextMapping.TryRemove(key);
                }
            }
        }

        #endregion
    }
}

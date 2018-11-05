using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Voguedi.Schedulers
{
    class Scheduler : IScheduler
    {
        #region Private Class

        class SchedulerContext
        {
            #region Public Properties

            public string Key { get; set; }

            public Action Action { get; set; }

            public Timer Timer { get; set; }

            public int DueTime { get; set; }

            public int Period { get; set; }

            public bool Started { get; set; }

            #endregion
        }

        #endregion

        #region Private Fields

        readonly ILogger logger;
        readonly object syncLock = new object();
        readonly ConcurrentDictionary<string, SchedulerContext> contextMapping = new ConcurrentDictionary<string, SchedulerContext>();

        #endregion

        #region Ctors

        public Scheduler(ILogger<Scheduler> logger) => this.logger = logger;

        #endregion

        #region Private Methods

        void Callback(object state)
        {
            var key = state.ToString();

            if (contextMapping.TryGetValue(key, out var context))
            {
                try
                {
                    if (context.Started)
                    {
                        context.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                        context.Action?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"调度器当前状态异常！ [Key = {context.Key}, DueTime = {context.DueTime}, Period = {context.Period}]");
                }
                finally
                {
                    try
                    {
                        if (context.Started)
                            context.Timer.Change(context.Period, context.Period);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"调度器当前状态异常！ [Key = {context.Key}, DueTime = {context.DueTime}, Period = {context.Period}]");
                    }
                }
            }
        }

        #endregion

        #region IScheduler

        public void Start(string key, Action action, int dueTime, int period)
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
                            DueTime = dueTime,
                            Key = key,
                            Period = period,
                            Started = true,
                            Timer = timer
                        });
                    timer.Change(dueTime, period);
                }
            }
        }

        public void Stop(string key)
        {
            lock (syncLock)
            {
                if (contextMapping.TryGetValue(key, out var context))
                {
                    context.Started = false;
                    context.Timer.Dispose();
                    contextMapping.TryRemove(key);
                }
            }
        }

        #endregion
    }
}

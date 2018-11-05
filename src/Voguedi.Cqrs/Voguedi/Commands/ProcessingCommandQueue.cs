using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Voguedi.Commands
{
    class ProcessingCommandQueue : IProcessingCommandQueue
    {
        #region Private Fields

        readonly string aggregateRootId;
        readonly IProcessingCommandHandler handler;
        readonly ILogger logger;
        readonly ConcurrentDictionary<long, ProcessingCommand> queue = new ConcurrentDictionary<long, ProcessingCommand>();
        readonly ConcurrentDictionary<long, ProcessingCommand> waitingQueue = new ConcurrentDictionary<long, ProcessingCommand>();
        readonly ManualResetEvent pausingHandle = new ManualResetEvent(false);
        readonly ManualResetEvent processingHandle = new ManualResetEvent(false);
        readonly object syncLock = new object();
        readonly AsyncLock asyncLock = new AsyncLock();
        const int starting = 1;
        const int stop = 0;
        const int timeout = 1000;
        int isStarting;
        bool isPausing;
        bool isProcessing;
        long previousSequence = -1;
        long currentSequence;
        long nextSequence;
        DateTime lastActiveOn;

        #endregion

        #region Ctors

        public ProcessingCommandQueue(string aggregateRootId, IProcessingCommandHandler handler, ILogger<ProcessingCommandQueue> logger)
        {
            this.aggregateRootId = aggregateRootId;
            this.handler = handler;
            this.logger = logger;
            lastActiveOn = DateTime.UtcNow;
        }

        #endregion

        #region Private Methods

        void TryStart()
        {
            if (Interlocked.CompareExchange(ref isStarting, starting, stop) == stop)
                Task.Factory.StartNew(async () => await StartAsync());
        }

        async Task StartAsync()
        {
            lastActiveOn = DateTime.UtcNow;

            while (isPausing)
            {
                logger.LogInformation($"命令处理队列当前状态为暂停，等待并重新启动！ [AggregateRootId = {aggregateRootId}]");
                pausingHandle.WaitOne(timeout);
            }

            var processingCommand = default(ProcessingCommand);

            try
            {
                processingHandle.Reset();
                isProcessing = true;

                while (currentSequence < nextSequence)
                {
                    if (queue.TryGetValue(currentSequence, out processingCommand) && processingCommand != null)
                        await handler.HandleAsync(processingCommand);

                    currentSequence++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"命令处理队列启动失败！ [AggregateRootId = {aggregateRootId}, CommandType = {processingCommand?.Command?.GetType()}, CommandId = {processingCommand?.Command?.Id}]");
                Thread.Sleep(1);
            }
            finally
            {
                isProcessing = false;
                processingHandle.Set();
                Stop();

                if (currentSequence < nextSequence)
                    TryStart();
            }
        }

        void Stop() => Interlocked.Exchange(ref isStarting, stop);

        async Task<long> CommitWaitingAsync(long committingSequence)
        {
            var commitedSequence = committingSequence;
            var waitingSequence = committingSequence + 1;
            var waitingProcessingCommand = default(ProcessingCommand);

            while (waitingQueue.ContainsKey(waitingSequence))
            {
                if (queue.TryRemove(waitingSequence, out waitingProcessingCommand))
                    await waitingProcessingCommand.OnConsumerCommittedAsync();

                waitingQueue.TryRemove(waitingSequence);
                commitedSequence = waitingSequence;
                waitingSequence++;
            }

            return commitedSequence;
        }

        async Task<long> RejectWaitingAsync(long rejectingSequence)
        {
            var rejectedSequence = rejectingSequence;
            var waitingSequence = rejectingSequence + 1;
            var waitingProcessingCommand = default(ProcessingCommand);

            while (waitingQueue.ContainsKey(waitingSequence))
            {
                if (queue.TryRemove(waitingSequence, out waitingProcessingCommand))
                    await waitingProcessingCommand.OnConsumerRejectedAsync();

                waitingQueue.TryRemove(waitingSequence);
                rejectedSequence = waitingSequence;
                waitingSequence++;
            }

            return rejectedSequence;
        }

        #endregion

        #region IProcessingCommandQueue

        public void Enqueue(ProcessingCommand processingCommand)
        {
            lock (syncLock)
            {
                processingCommand.QueueSequence = nextSequence;
                processingCommand.Queue = this;

                if (queue.TryAdd(processingCommand.QueueSequence, processingCommand))
                    nextSequence++;
            }

            lastActiveOn = DateTime.UtcNow;
            TryStart();
        }

        public void Pause()
        {
            lastActiveOn = DateTime.UtcNow;
            pausingHandle.Reset();

            while (isProcessing)
            {
                logger.LogInformation($"命令处理队列当前状态为已启动，等待并暂停！ [AggregateRootId = {aggregateRootId}]");
                processingHandle.WaitOne(timeout);
            }

            isPausing = true;
        }

        public void ResetSequence(long sequence)
        {
            lastActiveOn = DateTime.UtcNow;
            currentSequence = sequence;
            waitingQueue.Clear();
        }

        public void Restart()
        {
            lastActiveOn = DateTime.UtcNow;
            isPausing = false;
            pausingHandle.Set();
            TryStart();
        }

        public async Task CommitAsync(ProcessingCommand processingCommand)
        {
            using (await asyncLock.LockAsync())
            {
                lastActiveOn = DateTime.UtcNow;
                var command = processingCommand.Command;
                var committingSequence = processingCommand.QueueSequence;
                var exceptedSequence = previousSequence + 1;

                try
                {
                    if (committingSequence == exceptedSequence)
                    {
                        queue.TryRemove(processingCommand.QueueSequence, out var removed);
                        await processingCommand.OnConsumerCommittedAsync();
                        previousSequence = await CommitWaitingAsync(committingSequence);
                    }
                    else if (committingSequence > exceptedSequence)
                        waitingQueue[committingSequence] = processingCommand;
                    else
                    {
                        queue.TryRemove(committingSequence);
                        await processingCommand.OnConsumerCommittedAsync();
                        waitingQueue.TryRemove(committingSequence);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"命令处理队列提交失败！ [AggregateRootId = {aggregateRootId}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
                }
            }
        }

        public async Task RejectAsync(ProcessingCommand processingCommand)
        {
            using (await asyncLock.LockAsync())
            {
                lastActiveOn = DateTime.UtcNow;
                var command = processingCommand.Command;
                var rejectingSequence = processingCommand.QueueSequence;
                var exceptedSequence = previousSequence + 1;

                try
                {
                    if (rejectingSequence == exceptedSequence)
                    {
                        queue.TryRemove(processingCommand.QueueSequence, out var removed);
                        await processingCommand.OnConsumerRejectedAsync();
                        previousSequence = await RejectWaitingAsync(rejectingSequence);
                    }
                    else if (rejectingSequence > exceptedSequence)
                        await processingCommand.OnConsumerRejectedAsync();
                    else
                    {
                        queue.TryRemove(rejectingSequence);
                        await processingCommand.OnConsumerRejectedAsync();
                        waitingQueue.TryRemove(rejectingSequence);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"命令处理队列拒绝失败！ [AggregateRootId = {aggregateRootId}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
                }
            }
        }

        public bool IsInactive(int expiration) => (DateTime.UtcNow - lastActiveOn).TotalSeconds >= expiration && isStarting == starting;

        #endregion
    }
}

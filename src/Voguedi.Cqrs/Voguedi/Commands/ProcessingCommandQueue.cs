using System;
using System.Collections.Concurrent;
using System.Text;
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
        int isStarting = 0;
        bool isPausing = false;
        bool isProcessing = false;
        long previousSequence = -1;
        long currentSequence = 0;
        long nextSequence = 1;

        #endregion

        #region Ctors

        public ProcessingCommandQueue(string aggregateRootId, IProcessingCommandHandler handler, ILogger<ProcessingCommandQueue> logger)
        {
            this.aggregateRootId = aggregateRootId;
            this.handler = handler;
            this.logger = logger;
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
                logger.LogError(ex, $"命令处理队列启动失败，等待并重新启动！ [AggregateRootId = {aggregateRootId}, CommandType = {processingCommand?.Command?.GetType()}, CommandId = {processingCommand?.Command?.Id}]");
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

        async Task<long> CommitWaiting(long sequence)
        {
            var result = sequence;
            var next = sequence + 1;
            var waiting = default(ProcessingCommand);
            var removed = default(ProcessingCommand);

            while (waitingQueue.ContainsKey(next))
            {
                if (queue.TryRemove(next, out waiting))
                    await waiting.OnConsumerCommittedAsync();

                waitingQueue.TryRemove(next, out removed);
                result = next;
                next++;
            }

            return result;
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

            TryStart();
        }

        public void Pause()
        {
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
            currentSequence = sequence;
            waitingQueue.Clear();
        }

        public void Restart()
        {
            isPausing = false;
            pausingHandle.Set();
            TryStart();
        }

        public async Task CommitAsync(ProcessingCommand processingCommand)
        {
            using (await asyncLock.LockAsync())
            {
                var command = processingCommand.Command;
                var sequence = processingCommand.QueueSequence;

                try
                {
                    if (sequence == previousSequence + 1)
                    {
                        queue.TryRemove(processingCommand.QueueSequence, out var removed);
                        await processingCommand.OnConsumerCommittedAsync();

                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"命令处理队列提交失败！ [AggregateRootId = {aggregateRootId}, CommandType = {command.GetType()}, CommandId = {command.Id}]");
                }
            }
        }

        #endregion
    }
}

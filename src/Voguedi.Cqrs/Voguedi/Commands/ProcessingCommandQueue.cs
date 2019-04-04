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
        readonly ICommandExecutedResultProcessor executedResultProcessor;
        readonly ILogger logger;
        readonly ConcurrentDictionary<long, ProcessingCommand> queue;
        readonly ConcurrentDictionary<long, CommandExecutedResult> waitingQueue;
        readonly ManualResetEvent pausingHandle;
        readonly ManualResetEvent processingHandle;
        readonly object syncLock;
        readonly AsyncLock asyncLock;
        const int starting = 1;
        const int stop = 0;
        const int timeout = 1000;
        int isStarting;
        bool isPausing;
        bool isProcessing;
        long previousSequence;
        long currentSequence;
        long nextSequence;
        DateTime lastActiveOn;

        #endregion

        #region Ctors

        public ProcessingCommandQueue(
            string aggregateRootId,
            IProcessingCommandHandler handler,
            ICommandExecutedResultProcessor executedResultProcessor,
            ILogger<ProcessingCommandQueue> logger)
        {
            this.aggregateRootId = aggregateRootId;
            this.handler = handler;
            this.executedResultProcessor = executedResultProcessor;
            this.logger = logger;
            queue = new ConcurrentDictionary<long, ProcessingCommand>();
            waitingQueue = new ConcurrentDictionary<long, CommandExecutedResult>();
            pausingHandle = new ManualResetEvent(false);
            processingHandle = new ManualResetEvent(false);
            syncLock = new object();
            asyncLock = new AsyncLock();
            previousSequence = -1;
            lastActiveOn = DateTime.UtcNow;
        }

        #endregion

        #region Private Methods

        void TryStart()
        {
            if (Interlocked.CompareExchange(ref isStarting, starting, stop) == stop)
                Task.Factory.StartNew(StartAsync);
        }

        async Task StartAsync()
        {
            lastActiveOn = DateTime.UtcNow;

            while (isPausing)
            {
                logger.LogDebug($"队列当前状态为暂停，等待并重新启动。 [AggregateRootId = {aggregateRootId}]");
                pausingHandle.WaitOne(timeout);
            }

            var processingCommand = default(ProcessingCommand);

            try
            {
                processingHandle.Reset();
                isProcessing = true;

                while (currentSequence < nextSequence)
                {
                    if (queue.TryGetValue(currentSequence, out processingCommand))
                        await handler.HandleAsync(processingCommand);

                    currentSequence++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"处理队列启动失败。 [AggregateRootId = {aggregateRootId}, CommandType = {processingCommand?.Command?.GetType()}, CommandId = {processingCommand?.Command?.Id}]");
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

        async Task<long> ProcessWaitingAsync(long processingSequence)
        {
            var processedSequence = processingSequence;
            var waitingSequence = processingSequence + 1;

            while (waitingQueue.ContainsKey(waitingSequence))
            {
                if (queue.TryRemove(waitingSequence, out var waitingProcessingCommand))
                    await executedResultProcessor.ProcessAsync(waitingProcessingCommand, waitingQueue[waitingSequence]);

                waitingQueue.TryRemove(waitingSequence);
                processedSequence = waitingSequence;
                waitingSequence++;
            }

            return processedSequence;
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
                logger.LogDebug($"队列当前状态已启动，等待并暂停。 [AggregateRootId = {aggregateRootId}]");
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

        public async Task ProcessAsync(ProcessingCommand processingCommand, CommandExecutedResult executedResult)
        {
            using (await asyncLock.LockAsync())
            {
                lastActiveOn = DateTime.UtcNow;

                try
                {
                    if (processingCommand.QueueSequence == previousSequence + 1)
                    {
                        queue.TryRemove(processingCommand.QueueSequence);
                        previousSequence = await ProcessWaitingAsync(processingCommand.QueueSequence);
                    }
                    else if (processingCommand.QueueSequence > previousSequence + 1)
                        waitingQueue[processingCommand.QueueSequence] = executedResult;
                    else
                    {
                        queue.TryRemove(processingCommand.QueueSequence);
                        await executedResultProcessor.ProcessAsync(processingCommand, executedResult);
                        waitingQueue.TryRemove(processingCommand.QueueSequence);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"队列处理失败。 [AggregateRootId = {aggregateRootId}, CommandType = {processingCommand.Command.GetType()}, CommandId = {processingCommand.Command.Id}]");
                }
            }
        }

        public bool IsInactive(int expiration) => (DateTime.UtcNow - lastActiveOn).TotalSeconds >= expiration && isStarting == starting;

        #endregion
    }
}

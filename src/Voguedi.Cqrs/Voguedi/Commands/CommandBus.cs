using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Messaging;
using Voguedi.ObjectSerialization;

namespace Voguedi.Commands
{
    class CommandBus : ICommandBus
    {
        #region Private Fields

        readonly IMessageProducer producer;
        readonly IMessageQueueTopicProvider queueTopicProvider;
        readonly IStringObjectSerializer objectSerializer;
        readonly string defaultGroupName;
        readonly int defaultTopicQueueCount;
        readonly ConcurrentDictionary<Type, string> topicMapping = new ConcurrentDictionary<Type, string>();

        #endregion

        #region Ctors

        public CommandBus(IMessageProducer producer, IMessageQueueTopicProvider queueTopicProvider, IStringObjectSerializer objectSerializer, VoguediOptions options)
        {
            this.producer = producer;
            this.queueTopicProvider = queueTopicProvider;
            this.objectSerializer = objectSerializer;
            defaultGroupName = options.DefaultCommandGroupName;
            defaultTopicQueueCount = options.DefaultTopicQueueCount;
        }
        
        #endregion

        #region Private Methods

        string BuildQueueMessage(ICommand command)
        {
            var message = new CommandMessage
            {
                Content = objectSerializer.Serialize(command),
                Tag = command.GetType().AssemblyQualifiedName
            };
            return objectSerializer.Serialize(message);
        }

        #endregion

        #region ICommandBus

        Task<AsyncExecutedResult> ICommandSender.SendAsync<TCommand>(TCommand command)
            => producer.ProduceAsync(queueTopicProvider.Get(command, defaultGroupName, defaultTopicQueueCount), BuildQueueMessage(command));

        #endregion
    }
}

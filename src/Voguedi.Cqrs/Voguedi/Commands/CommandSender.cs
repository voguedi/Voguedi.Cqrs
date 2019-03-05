using System.Threading.Tasks;
using Voguedi.AsyncExecution;
using Voguedi.Messaging;
using Voguedi.ObjectSerializing;

namespace Voguedi.Commands
{
    class CommandSender : ICommandSender
    {
        #region Private Fields

        readonly IMessageProducer producer;
        readonly IMessageSubscriptionManager subscriptionManager;
        readonly IStringObjectSerializer objectSerializer;

        #endregion

        #region Ctors

        public CommandSender(IMessageProducer producer, IMessageSubscriptionManager subscriptionManager, IStringObjectSerializer objectSerializer)
        {
            this.producer = producer;
            this.subscriptionManager = subscriptionManager;
            this.objectSerializer = objectSerializer;
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

        #region ICommandSender

        Task<AsyncExecutedResult> ICommandSender.SendAsync<TCommand>(TCommand command) => producer.ProduceAsync(subscriptionManager.GetQueueTopic(command), BuildQueueMessage(command));

        #endregion
    }
}

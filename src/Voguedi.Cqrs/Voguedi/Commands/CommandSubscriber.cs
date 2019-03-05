using System;
using Microsoft.Extensions.Logging;
using Voguedi.Messaging;
using Voguedi.ObjectSerializing;

namespace Voguedi.Commands
{
    class CommandSubscriber : MessageSubscriber, ICommandSubscriber
    {
        #region Private Fields

        readonly ICommandProcessor processor;
        readonly IStringObjectSerializer objectSerializer;

        #endregion

        #region Ctors

        public CommandSubscriber(
            ICommandProcessor processor,
            IStringObjectSerializer objectSerializer,
            IMessageConsumerFactory consumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            ILogger<CommandSubscriber> logger,
            VoguediOptions options)
            : base(consumerFactory, subscriptionManager, logger, options.DefaultCommandGroupName, options.DefaultTopicQueueCount)
        {
            this.processor = processor;
            this.objectSerializer = objectSerializer;
        }

        #endregion

        #region MessageSubscriber

        protected override Type GetMessageBaseType() => typeof(ICommand);

        protected override void Process(ReceivingMessage receivingMessage, IMessageConsumer consumer)
        {
            var message = objectSerializer.Deserialize<CommandMessage>(receivingMessage.QueueMessage);
            var command = (ICommand)objectSerializer.Deserialize(message.Content, Type.GetType(message.Tag));
            processor.Process(new ProcessingCommand(command, consumer));
        }

        #endregion
    }
}

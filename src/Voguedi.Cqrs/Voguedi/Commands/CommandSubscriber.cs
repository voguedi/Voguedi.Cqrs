using Microsoft.Extensions.Logging;
using Voguedi.MessageQueues;
using Voguedi.Messaging;

namespace Voguedi.Commands
{
    class CommandSubscriber : MessageSubscriber<ICommand>, ICommandSubscriber
    {
        #region Ctors

        public CommandSubscriber(
            IMessageQueueConsumerFactory queueConsumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            ICommandProcessor processor,
            ILogger<CommandSubscriber> logger,
            VoguediOptions options)
            : base(queueConsumerFactory, subscriptionManager, processor, logger, options.DefaultCommandGroupName, options.DefaultCommandTopicQueueCount) { }

        #endregion
    }
}

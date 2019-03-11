using Microsoft.Extensions.Logging;
using Voguedi.Messaging;

namespace Voguedi.Commands
{
    class CommandSubscriber : MessageSubscriber<ICommand>, ICommandSubscriber
    {
        #region Ctors

        public CommandSubscriber(
            IMessageConsumerFactory consumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            ICommandProcessor processor,
            ILogger<CommandSubscriber> logger,
            VoguediOptions options)
            : base(consumerFactory, subscriptionManager, processor, logger, options.DefaultCommandGroupName, options.DefaultCommandTopicQueueCount) { }

        #endregion
    }
}

using Microsoft.Extensions.Logging;
using Voguedi.MessageQueues;
using Voguedi.Messaging;

namespace Voguedi.ApplicationMessages
{
    class ApplicationMessageSubscriber : MessageSubscriber<IApplicationMessage>, IApplicationMessageSubscriber
    {
        #region Ctors

        public ApplicationMessageSubscriber(
            IMessageQueueConsumerFactory queueConsumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            IApplicationMessageProcessor processor,
            ILogger<ApplicationMessageSubscriber> logger,
            VoguediOptions options)
            : base(queueConsumerFactory, subscriptionManager, processor, logger, options.DefaultApplicationMessageGroupName, options.DefaultApplicationTopicQueueCount) { }

        #endregion
    }
}

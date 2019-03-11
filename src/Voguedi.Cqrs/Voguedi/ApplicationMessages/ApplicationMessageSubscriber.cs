using Microsoft.Extensions.Logging;
using Voguedi.Messaging;

namespace Voguedi.ApplicationMessages
{
    class ApplicationMessageSubscriber : MessageSubscriber<IApplicationMessage>, IApplicationMessageSubscriber
    {
        #region Ctors

        public ApplicationMessageSubscriber(
            IMessageConsumerFactory consumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            IApplicationMessageProcessor processor,
            ILogger<ApplicationMessageSubscriber> logger,
            VoguediOptions options)
            : base(consumerFactory, subscriptionManager, processor, logger, options.DefaultApplicationMessageGroupName, options.DefaultApplicationTopicQueueCount) { }

        #endregion
    }
}

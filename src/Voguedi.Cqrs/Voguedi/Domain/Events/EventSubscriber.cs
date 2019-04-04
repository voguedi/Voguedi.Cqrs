using Microsoft.Extensions.Logging;
using Voguedi.MessageQueues;
using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    class EventSubscriber : MessageSubscriber<IEvent>, IEventSubscriber
    {
        #region Ctors

        public EventSubscriber(
            IMessageQueueConsumerFactory queueConsumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            IEventProcessor processor,
            ILogger<EventSubscriber> logger,
            VoguediOptions options)
            : base(queueConsumerFactory, subscriptionManager, processor, logger, options.DefaultEventGroupName, options.DefaultEventTopicQueueCount) { }

        #endregion
    }
}

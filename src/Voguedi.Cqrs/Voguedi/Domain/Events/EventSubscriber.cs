using Microsoft.Extensions.Logging;
using Voguedi.Messaging;

namespace Voguedi.Domain.Events
{
    class EventSubscriber : MessageSubscriber<IEvent>, IEventSubscriber
    {
        #region Ctors

        public EventSubscriber(
            IMessageConsumerFactory consumerFactory,
            IMessageSubscriptionManager subscriptionManager,
            IEventProcessor processor,
            ILogger<EventSubscriber> logger,
            VoguediOptions options)
            : base(consumerFactory, subscriptionManager, processor, logger, options.DefaultEventGroupName, options.DefaultEventTopicQueueCount) { }

        #endregion
    }
}

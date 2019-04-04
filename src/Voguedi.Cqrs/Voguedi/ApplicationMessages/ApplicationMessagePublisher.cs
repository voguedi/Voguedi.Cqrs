using System.Threading.Tasks;
using Voguedi.Infrastructure;
using Voguedi.Messaging;

namespace Voguedi.ApplicationMessages
{
    class ApplicationMessagePublisher : IApplicationMessagePublisher
    {
        #region Private Fields

        readonly IMessagePublisher publisher;

        #endregion

        #region Ctors

        public ApplicationMessagePublisher(IMessagePublisher publisher) => this.publisher = publisher;

        #endregion

        #region IApplicationMessagePublisher

        public Task<AsyncExecutedResult> PublishAsync(IApplicationMessage applicationMessage) => publisher.PublishAsync(applicationMessage);

        #endregion
    }
}

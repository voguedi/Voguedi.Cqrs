using System;

namespace Voguedi.Messaging
{
    public interface IMessage
    {
        #region Properties

        long Id { get; set; }

        DateTime Timestamp { get; set; }

        #endregion

        #region Methods

        string GetRoutingKey();

        #endregion
    }
}

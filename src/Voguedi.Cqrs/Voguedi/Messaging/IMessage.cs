using System;

namespace Voguedi.Messaging
{
    public interface IMessage
    {
        #region Properties

        string Id { get; set; }

        DateTime Timestamp { get; set; }

        #endregion

        #region Methods

        string GetTypeName();

        string GetRoutingKey();

        #endregion
    }
}

using System;
using System.Threading;

namespace ServiceBroker
{
    public interface IQueueMessageListener
    {
        event EventHandler<MessagesReceivedEventArgs> MessagesReceived;

        void Listen(CancellationToken cancellationToken = default(CancellationToken));
    }
}

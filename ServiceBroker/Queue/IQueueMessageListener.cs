using System;
using System.Threading;

namespace ServiceBroker
{
    public interface IQueueMessageListener
    {
        void Listen(CancellationToken cancellationToken = default(CancellationToken));
    }
}

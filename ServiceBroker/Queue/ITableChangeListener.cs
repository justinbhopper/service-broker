using System;
using System.Threading;

namespace ServiceBroker
{
    public interface ITableChangeListener
    {
        event EventHandler<TablesChangedEventArgs> TablesChanged;

        void Listen(CancellationToken cancellationToken = default(CancellationToken));
    }
}

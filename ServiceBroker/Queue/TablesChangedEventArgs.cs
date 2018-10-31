using System;
using System.Collections.Generic;

namespace ServiceBroker
{
    public class TablesChangedEventArgs : EventArgs
    {
        public TablesChangedEventArgs(IEnumerable<TableChange> changes)
        {
            Changes = changes;
        }

        public IEnumerable<TableChange> Changes { get; }
    }
}

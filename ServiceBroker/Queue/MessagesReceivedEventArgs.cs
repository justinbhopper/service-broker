using System;
using System.Collections.Generic;

namespace ServiceBroker
{
    public class MessagesReceivedEventArgs : EventArgs
    {
        public MessagesReceivedEventArgs(IEnumerable<Message> messages)
        {
            Messages = messages;
        }

        public IEnumerable<Message> Messages { get; }
    }
}

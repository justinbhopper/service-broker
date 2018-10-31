using System;

namespace ServiceBroker
{
    public class Message
    {
        public Guid ConversationHandle { get; set; }
        public byte[] Body { get; set; }
    }
}

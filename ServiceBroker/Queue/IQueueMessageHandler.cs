using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public interface IQueueMessageHandler
    {
        Task HandleAsync(IEnumerable<Message> messages);
    }
}

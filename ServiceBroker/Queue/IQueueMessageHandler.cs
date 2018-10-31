using System.Threading.Tasks;

namespace ServiceBroker
{
    public interface IQueueMessageHandler
    {
        Task HandleAsync(Message message);
    }
}

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class TableChangeQueueMessageHandler : IQueueMessageHandler
    {
        private readonly TableChangeSerializer _serializer = new TableChangeSerializer();
        private readonly ITableChangeHandler _changeHandler;

        public TableChangeQueueMessageHandler(ITableChangeHandler changeHandler)
        {
            _changeHandler = changeHandler;
        }
        
        public async Task HandleAsync(Message message)
        {
            var tableChanges = _serializer.Deserialize(message.Body, Encoding.Unicode);

            if (tableChanges.Any())
                await _changeHandler.HandleAsync(tableChanges);
        }
    }
}

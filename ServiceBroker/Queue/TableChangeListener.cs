using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace ServiceBroker
{
    public class TableChangeListener : ITableChangeListener
    {
        private readonly TableChangeSerializer _serializer = new TableChangeSerializer();
        private readonly IQueueMessageListener _messageListener;

        public TableChangeListener(IQueueMessageListener messageListener)
        {
            _messageListener = messageListener;
            _messageListener.MessagesReceived += OnMessagesReceived;
        }

        public event EventHandler<TablesChangedEventArgs> TablesChanged;

        public void Listen(CancellationToken cancellationToken = default(CancellationToken))
        {
            _messageListener.Listen(cancellationToken);
        }

        private void OnMessagesReceived(object sender, MessagesReceivedEventArgs e)
        {
            foreach (var message in e.Messages)
            {
                HandleMessage(message);
            }
        }

        private void HandleMessage(Message message)
        {
            var handler = TablesChanged;
            if (handler != null)
            {
                var tableChanges = _serializer.Deserialize(message.Body, Encoding.Unicode);

                if (tableChanges.Any())
                    handler(this, new TablesChangedEventArgs(tableChanges));
            }
        }
    }
}

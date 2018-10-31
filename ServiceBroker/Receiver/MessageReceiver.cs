using System;
using System.Data.SqlClient;
using System.Threading;

namespace ServiceBroker
{
    public class MessageReceiver
    {
        private readonly ITableChangeListener _tableChangeListener;
        private readonly CancellationTokenSource _tokenSource;
        private readonly int? _numberOfMessages;

        private int _messageCount;

        public MessageReceiver(SqlConnection sqlConnection, int? numberOfMessages)
        {
            _numberOfMessages = numberOfMessages;
            
            var messageListener = new QueueMessageListener(sqlConnection);
            messageListener.MessagesReceived += OnMessagesReceived;

            _tableChangeListener = new TableChangeListener(messageListener);
            _tableChangeListener.TablesChanged += OnTablesChanged;

            _tokenSource = new CancellationTokenSource();
        }

        public event EventHandler<TablesChangedEventArgs> TablesChanged;
        public event EventHandler Finished;

        public int Received { get { return _messageCount; } }
        public int Failures { get; private set; }
        public DateTime? FinishedTime { get; private set; }
        public DateTime StartTime { get; private set; }
        
        public void Listen(CancellationToken cancellation = default(CancellationToken))
        {
            OnStart();
            
            cancellation.Register(SignalStop);
            _tableChangeListener.Listen(_tokenSource.Token);
        }

        private void OnTablesChanged(object sender, TablesChangedEventArgs e)
        {
            TablesChanged?.Invoke(sender, e);
        }

        private void SignalStop()
        {
            _tokenSource.Cancel();
            OnFinished();
        }

        private void OnMessagesReceived(object sender, MessagesReceivedEventArgs e)
        {
            int count = Interlocked.Increment(ref _messageCount);

            if (_numberOfMessages.HasValue && count >= _numberOfMessages.Value)
            {
                SignalStop();
            }
        }
        
        private void OnStart()
        {
            _messageCount = 0;
            StartTime = DateTime.Now;
        }

        private void OnFinished()
        {
            FinishedTime = DateTime.Now;
            Finished?.Invoke(this, EventArgs.Empty);
        }
    }
}

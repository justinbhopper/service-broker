using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class MessageReceiver : IQueueMessageHandler
    {
        private readonly IQueueMessageListener _messageListener;
        private readonly IQueueMessageHandler _messageHandler;
        private readonly CancellationTokenSource _tokenSource;
        private readonly int? _numberOfMessages;

        private int _messageCount;

        public MessageReceiver(SqlConnection sqlConnection)
            : this(sqlConnection, null, null) { }

        public MessageReceiver(SqlConnection sqlConnection, int numberOfMessages)
            : this(sqlConnection, null, numberOfMessages) { }

        public MessageReceiver(SqlConnection sqlConnection, IQueueMessageHandler messageHandler)
            : this(sqlConnection, messageHandler, null) { }

        public MessageReceiver(SqlConnection sqlConnection, IQueueMessageHandler messageHandler, int numberOfMessages)
            : this(sqlConnection, messageHandler, (int?)numberOfMessages) { }

        private MessageReceiver(SqlConnection sqlConnection, IQueueMessageHandler messageHandler, int? numberOfMessages)
        {
            _numberOfMessages = numberOfMessages;
            _tokenSource = new CancellationTokenSource();

            _messageHandler = messageHandler;
            _messageListener = new QueueMessageListener(sqlConnection, this);
        }

        public event EventHandler MessageReceived;
        public event EventHandler Finished;

        public int Received { get { return _messageCount; } }
        public int Failures { get; private set; }
        public DateTime? FinishedTime { get; private set; }
        public DateTime StartTime { get; private set; }
        
        public void Listen(CancellationToken cancellation = default(CancellationToken))
        {
            OnStart();
            
            cancellation.Register(SignalStop);
            _messageListener.Listen(_tokenSource.Token);
        }

        private void SignalStop()
        {
            _tokenSource.Cancel();
            OnFinished();
        }

        async Task IQueueMessageHandler.HandleAsync(Message message)
        {
            int count = Interlocked.Increment(ref _messageCount);

            if (_messageHandler != null)
                await _messageHandler.HandleAsync(message);

            MessageReceived?.Invoke(this, EventArgs.Empty);

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

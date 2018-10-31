using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class MessageReceiver : IQueueMessageHandler
    {
        private readonly IQueueMessageListener _messageListener;
        private readonly IQueueMessageHandler _tableChangeHandler;
        private readonly CancellationTokenSource _tokenSource;
        private readonly int? _numberOfMessages;

        private int _messageCount;

        public MessageReceiver(SqlConnection sqlConnection)
            : this(sqlConnection, null, null) { }

        public MessageReceiver(SqlConnection sqlConnection, int numberOfMessages)
            : this(sqlConnection, null, numberOfMessages) { }

        public MessageReceiver(SqlConnection sqlConnection, ITableChangeHandler changeHandler)
            : this(sqlConnection, changeHandler, null) { }

        public MessageReceiver(SqlConnection sqlConnection, ITableChangeHandler changeHandler, int numberOfMessages)
            : this(sqlConnection, changeHandler, (int?)numberOfMessages) { }

        private MessageReceiver(SqlConnection sqlConnection, ITableChangeHandler changeHandler, int? numberOfMessages)
        {
            _numberOfMessages = numberOfMessages;
            _tokenSource = new CancellationTokenSource();

            _tableChangeHandler = changeHandler != null ? new TableChangeQueueMessageHandler(changeHandler) : null;
            _messageListener = new QueueMessageListener(sqlConnection, this);
        }

        public event EventHandler MessagesReceived;
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

        async Task IQueueMessageHandler.HandleAsync(IEnumerable<Message> messages)
        {
            int count = Interlocked.Increment(ref _messageCount);

            if (_tableChangeHandler != null)
                await _tableChangeHandler.HandleAsync(messages);

            MessagesReceived?.Invoke(this, EventArgs.Empty);

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

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class MultipleReadersTest : IDisposable, ITableChangeHandler
    {
        private readonly IList<MessageReceiver> _receivers = new List<MessageReceiver>();
        private readonly MessageSender _sender;
        private readonly PerformanceStats _stats;
        private readonly IList<SqlConnection> _receiveConnections = new List<SqlConnection>();
        private readonly SqlConnection _sendConnection;
        private readonly PreTest _preTest;
        private readonly CancellationTokenSource _token = new CancellationTokenSource();
        private readonly int _numberOfMessages;
        private readonly Action _checkIfFinished;

        public MultipleReadersTest(string sendConnectionString, string receiveConnectionString, int numberOfReaders, int numberOfMessages)
        {
            _sendConnection = new SqlConnection(sendConnectionString);
            _numberOfMessages = numberOfMessages;
            _checkIfFinished = new Action(() => CheckIfFinished()).Debounce(TimeSpan.FromMilliseconds(500));

            for (int i = 0; i < numberOfReaders; ++i)
            {
                var ids = new HashSet<int>();
                var connection = new SqlConnection(receiveConnectionString);
                var receiver = new MessageReceiver(connection, this);
                
                _receivers.Add(receiver);
                _receiveConnections.Add(connection);
            }
            
            _preTest = new PreTest(_sendConnection);
            _sender = new MessageSender(_sendConnection, numberOfMessages);
            _stats = new PerformanceStats(_sender, _receivers);
        }

        public event EventHandler<string> UpdateStatistics;

        public event EventHandler Finished;

        public void Start()
        {
            _preTest.Execute();
            _stats.UpdateStatistics += OnUpdateStatistics;

            _sender.Finished += (s, e) => OnSendingFinished();
            _sender.Start();
        }

        public void Dispose()
        {
            foreach (var receiveConnection in _receiveConnections)
            {
                receiveConnection.Dispose();
            }

            _sendConnection.Dispose();
        }

        private void OnSendingFinished()
        {
            foreach (var receiver in _receivers)
            {
                receiver.Listen(_token.Token);
            }
        }

        Task ITableChangeHandler.HandleAsync(IEnumerable<TableChange> tableChanges)
        {
            _checkIfFinished();
            return Task.CompletedTask;
        }

        private void CheckIfFinished()
        {
            if (_receivers.Sum(r => r.Received) < _numberOfMessages)
                return;

            // Signal stop
            _token.Cancel();

            UpdateStatistics?.Invoke(this, _stats.Stats);
            _stats.UpdateStatistics -= OnUpdateStatistics;
            Finished?.Invoke(this, EventArgs.Empty);
        }

        private void OnUpdateStatistics(object sender, string stats)
        {
            UpdateStatistics?.Invoke(this, stats);
        }
    }
}

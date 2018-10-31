using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace ServiceBroker
{
    public class MultipleReadersTest : IDisposable
    {
        private readonly IList<MessageReceiver> _receivers;
        private readonly MessageSender _sender;
        private readonly PerformanceStats _stats;
        private readonly IList<SqlConnection> _receiveConnections;
        private readonly SqlConnection _sendConnection;
        private readonly PreTest _preTest;
        private readonly CancellationTokenSource _token = new CancellationTokenSource();
        private readonly int _numberOfMessages;
        private Action _checkIfFinished;

        public MultipleReadersTest(string sendConnectionString, string receiveConnectionString, int numberOfReaders, int numberOfMessages)
        {
            _sendConnection = new SqlConnection(sendConnectionString);
            _receiveConnections = new List<SqlConnection>();
            _numberOfMessages = numberOfMessages;

            for (int i = 0; i < numberOfReaders; ++i)
            {
                _receiveConnections.Add(new SqlConnection(receiveConnectionString));
            }

            _preTest = new PreTest(_sendConnection);
            _sender = new MessageSender(_sendConnection, numberOfMessages);
            _receivers = _receiveConnections.Select(c => new MessageReceiver(c, null)).ToList();
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
            _checkIfFinished = new Action(() => CheckIfFinished()).Debounce(TimeSpan.FromMilliseconds(500));

            foreach (var receiver in _receivers)
            {
                receiver.TablesChanged += (s, e) => _checkIfFinished();
                receiver.Listen(_token.Token);
            }
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

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceBroker
{
    public class ConcurrencyProblemTest : IDisposable
    {
        private readonly IList<MessageReceiver> _receivers = new List<MessageReceiver>();
        private readonly MessageSender _sender;
        private readonly PerformanceStats _stats;
        private readonly IList<SqlConnection> _receiveConnections = new List<SqlConnection>();
        private readonly SqlConnection _sendConnection;
        private readonly PreTest _preTest;
        private readonly IList<ISet<int>> _clientIds = new List<ISet<int>>();
        private readonly CancellationTokenSource _token = new CancellationTokenSource();
        private readonly int _numberOfMessages;
        private readonly Action _checkIfFinished;

        public ConcurrencyProblemTest(string sendConnectionString, string receiveConnectionString, int numberOfReaders, int numberOfMessages)
        {
            _sendConnection = new SqlConnection(sendConnectionString);
            _numberOfMessages = numberOfMessages;
            _checkIfFinished = new Action(() => CheckIfFinished()).Debounce(TimeSpan.FromMilliseconds(500));

            for (int i = 0; i < numberOfReaders; ++i)
            {
                var ids = new HashSet<int>();
                var connection = new SqlConnection(receiveConnectionString);
                var collector = new TableChangeIdCollector(ids);
                var messageHandler = new TableChangeQueueMessageHandler(collector);
                var receiver = new MessageReceiver(connection, messageHandler);

                collector.TableChanged += (s, e) => _checkIfFinished();

                _receivers.Add(receiver);
                _clientIds.Add(ids);
                _receiveConnections.Add(connection);
            }

            _preTest = new PreTest(_sendConnection);
            _sender = new MessageSender(_sendConnection, numberOfMessages);
            _stats = new PerformanceStats(_sender, _receivers);
        }

        public event EventHandler<string> UpdateStatistics;

        public event EventHandler<int> Finished;

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
            for (int i = 0; i < _receivers.Count; i++)
            {
                _clientIds[i].Clear();
                _receivers[i].Listen(_token.Token);
            }
        }
        
        private void CheckIfFinished()
        {
            if (_receivers.Sum(r => r.Received) < _numberOfMessages)
                return;

            // Signal stop
            _token.Cancel();

            UpdateStatistics?.Invoke(this, _stats.Stats);
            OnFinished();
        }
        
        private void OnFinished()
        {
            int numberOfConflicts = 0;
            for (int i = 0; i < _clientIds.Count; i++)
            {
                var clientId = _clientIds[i];

                for (int k = 0; k < _clientIds.Count; k++)
                {
                    if (k == i)
                        continue;

                    var compareWithIds = _clientIds[k];
                    
                    numberOfConflicts += clientId.Intersect(compareWithIds).Count();
                }
            }

            _stats.UpdateStatistics -= OnUpdateStatistics;
            Finished?.Invoke(this, numberOfConflicts);
        }
        
        private void OnUpdateStatistics(object sender, string stats)
        {
            UpdateStatistics?.Invoke(this, stats);
        }

        private class TableChangeIdCollector : ITableChangeHandler
        {
            private readonly ISet<int> _ids;

            public TableChangeIdCollector(ISet<int> ids)
            {
                _ids = ids;
            }

            public event EventHandler TableChanged;

            Task ITableChangeHandler.HandleAsync(IEnumerable<TableChange> tableChanges)
            {
                foreach (var tableChange in tableChanges)
                {
                    if (tableChange.InsertedOrUpdated == null)
                        continue;
                    
                    foreach (int id in tableChange.InsertedOrUpdated)
                    {
                        _ids.Add(id);
                    }
                }

                TableChanged?.Invoke(this, EventArgs.Empty);

                return Task.CompletedTask;
            }
        }
    }
}

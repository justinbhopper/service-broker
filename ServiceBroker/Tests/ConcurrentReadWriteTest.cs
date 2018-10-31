using System;
using System.Data.SqlClient;

namespace ServiceBroker
{
    public class ConcurrentReadWriteTest : IDisposable
    {
        private readonly MessageReceiver _receiver;
        private readonly MessageSender _sender;
        private readonly PerformanceStats _stats;
        private readonly SqlConnection _receiveConnection;
        private readonly SqlConnection _sendConnection;
        private readonly PreTest _preTest;

        public ConcurrentReadWriteTest(string sendConnectionString, string receiveConnectionString, int numberOfMessages)
        {
            _sendConnection = new SqlConnection(sendConnectionString);
            _receiveConnection = new SqlConnection(receiveConnectionString);

            _preTest = new PreTest(_sendConnection);

            _sender = new MessageSender(_sendConnection, numberOfMessages);

            _receiver = new MessageReceiver(_receiveConnection, numberOfMessages);
            _receiver.Finished += OnReceivingFinished;

            _stats = new PerformanceStats(_sender, new[] { _receiver });
        }

        public event EventHandler<string> UpdateStatistics;

        public event EventHandler Finished;

        public void Start()
        {
            _preTest.Execute();
            _stats.UpdateStatistics += OnUpdateStatistics;

            _receiver.Listen();
            _sender.Start();
        }

        public void Dispose()
        {
            _receiveConnection.Dispose();
            _sendConnection.Dispose();
        }

        private void OnReceivingFinished(object sender, EventArgs eventArgs)
        {
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

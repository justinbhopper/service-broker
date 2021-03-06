﻿using System;
using System.Data.SqlClient;

namespace ServiceBroker
{
    public class AsyncProcessingTest : IDisposable
    {
        private readonly MessageReceiver _receiver;
        private readonly MessageSender _sender;
        private readonly PerformanceStats _stats;
        private readonly SqlConnection _receiveConnection;
        private readonly SqlConnection _sendConnection;
        private readonly MockSynchronizer _synchronizer = new MockSynchronizer();
        private readonly PreTest _preTest;

        public AsyncProcessingTest(string sendConnectionString, string receiveConnectionString, int numberOfMessages, int maxParallelProcessing)
        {
            _sendConnection = new SqlConnection(sendConnectionString);
            _receiveConnection = new SqlConnection(receiveConnectionString);

            _preTest = new PreTest(_sendConnection);

            var changeHandler = new TableChangeHandler(new MockSynchronizerMapper(_synchronizer));
            var messageHandler = new TableChangeQueueMessageHandler(changeHandler);
            var parallelMessageHandler = new ParallelQueueMessageHandler(messageHandler, maxParallelProcessing);

            _sender = new MessageSender(_sendConnection, numberOfMessages);
            _receiver = new MessageReceiver(_receiveConnection, parallelMessageHandler, numberOfMessages);

            _stats = new PerformanceStats(_sender, new[] { _receiver });
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
            _receiveConnection.Dispose();
            _sendConnection.Dispose();
        }

        private void OnSendingFinished()
        {
            _receiver.Finished += OnReceivingFinished;
            _receiver.Listen();
        }
        
        private void OnReceivingFinished(object sender, EventArgs eventArgs)
        {
            UpdateStatistics?.Invoke(this, _stats.Stats);

            _stats.UpdateStatistics -= OnUpdateStatistics;
            Finished?.Invoke(this, _synchronizer.Processed);
        }

        private void OnUpdateStatistics(object sender, string stats)
        {
            UpdateStatistics?.Invoke(this, stats);
        }
    }
}

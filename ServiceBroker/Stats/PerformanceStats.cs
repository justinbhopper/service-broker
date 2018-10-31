using System;
using System.Collections.Generic;

namespace ServiceBroker
{
    public class PerformanceStats
    {
        private readonly Action _updateStats;
        private readonly ReceiverStats _receiverStats;
        private readonly MessageSender _sender;

        public PerformanceStats(MessageSender sender, IEnumerable<MessageReceiver> receivers)
        {
            _sender = sender;
            _receiverStats = new ReceiverStats(receivers);
            _updateStats = new Action(OnUpdateStatistics).Debounce(TimeSpan.FromMilliseconds(100));

            _sender.MessageSent += (s, e) => OnStatsChanged();
            _sender.FailedMessageSend += (s, e) => OnStatsChanged();
            _receiverStats.UpdateStatistics += (s, e) => OnStatsChanged();

            foreach (var receiver in receivers)
            {
                receiver.MessageReceived += (s, e) => OnStatsChanged();
            }
        }

        public event EventHandler<string> UpdateStatistics;

        public string Stats
        {
            get
            {
                double sendRuntimeSec = ((_sender.FinishedTime ?? DateTime.Now) - _sender.StartTime).TotalSeconds;
                int sentCount = _sender.Sent;
                int sentFail = _sender.Failures;

                double receiveRuntimeSec = (DateTime.Now - _receiverStats.StartTime).TotalSeconds;
                int recievedCount = _receiverStats.TotalRecievedCount;
                int recievedFail = _receiverStats.TotalFailedCount;

                double totalRuntimeSec = sendRuntimeSec + (recievedCount > 0 ? receiveRuntimeSec : 0);
                
                return
                    $"Seconds Elapsed: {totalRuntimeSec.ToString("N1")}, Read/Write Disparity: {sentCount - recievedCount}\n" +
                    $"Sent: {sentCount}, Time: {sendRuntimeSec.ToString("N1")} sec, Failed: {sentFail}, Speed: {(sentCount / sendRuntimeSec).ToString("N0")} msgs per second\n" +
                    _receiverStats.Stats +
                    $"Total Received: {recievedCount}, Failed: {recievedFail}, Speed: {(recievedCount / receiveRuntimeSec).ToString("N0")} msgs per second";
            }
        }

        private void OnStatsChanged()
        {
            _updateStats();
        }
        
        private void OnUpdateStatistics()
        {
            UpdateStatistics?.Invoke(this, Stats);
        }
    }
}

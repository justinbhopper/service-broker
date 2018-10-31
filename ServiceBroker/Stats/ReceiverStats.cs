using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceBroker
{
    public class ReceiverStats
    {
        private readonly IEnumerable<MessageReceiver> _receivers;

        public ReceiverStats(IEnumerable<MessageReceiver> receivers)
        {
            _receivers = receivers;

            foreach (var receiver in receivers)
            {
                receiver.TablesChanged += (s, e) => OnStatsChanged();
            }
        }

        public event EventHandler UpdateStatistics;

        public int TotalRecievedCount { get { return _receivers.Sum(r => r.Received); } }
        public int TotalFailedCount { get { return _receivers.Sum(r => r.Failures); } }
        public DateTime StartTime { get { return _receivers.Min(r => r.StartTime); } }

        public string Stats
        {
            get
            {
                var sb = new StringBuilder();

                foreach (var receiver in _receivers)
                {
                    double receiveRuntimeSec = ((receiver.FinishedTime ?? DateTime.Now) - receiver.StartTime).TotalSeconds;
                    int recievedCount = receiver.Received;
                    int recievedFail = receiver.Failures;

                    sb.AppendLine($"Recieved: {recievedCount}, Failed: {recievedFail}, Speed: {(recievedCount / receiveRuntimeSec).ToString("N0")} msgs per second");
                }
                
                return sb.ToString();
            }
        }

        private void OnStatsChanged()
        {
            OnUpdateStatistics();
        }
        
        private void OnUpdateStatistics()
        {
            UpdateStatistics?.Invoke(this, EventArgs.Empty);
        }
    }
}

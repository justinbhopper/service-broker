using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;

namespace ServiceBroker
{
    public class MessageSender
    {
        private readonly SqlConnection _sqlConnection;
        private readonly int _numberOfMessages;

        public MessageSender(SqlConnection sqlConnection, int numberOfMessages)
        {
            _sqlConnection = sqlConnection;
            _numberOfMessages = numberOfMessages;
        }

        public event EventHandler MessageSent;
        public event EventHandler FailedMessageSend;
        public event EventHandler Finished;

        public int Sent { get; private set; }
        public int Failures { get; private set; }
        public DateTime? FinishedTime { get; private set; }
        public DateTime StartTime { get; private set; }

        public void Start()
        {
            FinishedTime = null;
            StartTime = DateTime.Now;

            Task.Factory.StartNew(Loop, TaskCreationOptions.LongRunning);
        }

        private async Task Loop()
        {
            for (Sent = 0; Sent < _numberOfMessages; Sent++)
            {
                await SendMessagesAsync();
            }

            FinishedTime = DateTime.Now;
            Finished?.Invoke(this, EventArgs.Empty);
        }

        private async Task SendMessagesAsync()
        {
            try
            {
                var command = new CommandDefinition("dbo.spx_InsertClient", new { clientKey = Guid.NewGuid() }, commandType: CommandType.StoredProcedure);
                
                await _sqlConnection.ExecuteAsync(command);
                MessageSent?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                Failures++;
                FailedMessageSend?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

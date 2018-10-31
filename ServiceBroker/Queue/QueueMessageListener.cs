using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Dapper;

namespace ServiceBroker
{
    public class QueueMessageListener : IQueueMessageListener
    {
        private readonly SqlConnection _sqlConnection;
        private readonly CommandDefinition _command;

        public QueueMessageListener(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
            _command = new CommandDefinition("dbo.spx_ReceiveSyncRequest", new { timeoutMs = 100 }, commandTimeout: 2, commandType: CommandType.StoredProcedure);
        }

        public event EventHandler<MessagesReceivedEventArgs> MessagesReceived;

        public void Listen(CancellationToken cancellationToken = default(CancellationToken))
        {
            var thread = new Thread(() => LoopUntil(cancellationToken)) { IsBackground = true };
            thread.Start();
        }
        
        private void LoopUntil(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                CheckForMessages();
            }
        }

        private void CheckForMessages()
        {
            try
            {
                var messages = _sqlConnection.Query<Message>(_command);

                int count = messages.Count();

                if (count > 0)
                {
                    MessagesReceived?.Invoke(this, new MessagesReceivedEventArgs(messages));
                }
            }
            catch (Exception e)
            {
                // TODO: add an event here
            }
        }
    }
}
